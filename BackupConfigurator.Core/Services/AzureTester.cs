using BackupConfigurator.Core.Models;
using Serilog;
using System.Globalization;

namespace BackupConfigurator.Core.Services;

public class AzureTester
{
    private readonly ILogger _logger;

    public AzureTester(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> TestConnectionAsync(BackupConfiguration config)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Testing Azure Blob connection to {ContainerUrl}", config.AzureContainerUrl);

            var containerUriWithSas = BuildContainerUri(config);
            var displayUri = RedactSasSignature(containerUriWithSas);
            result.AddDetail($"Testing connection to: {displayUri}");

            var sigValidation = ValidateSasSignature(containerUriWithSas);
            if (!sigValidation.Success)
            {
                result.Success = false;
                result.Message = sigValidation.Message;
                return result;
            }

            var permsValidation = ValidateSasPermissionsAndTimes(containerUriWithSas);
            if (!permsValidation.Success)
            {
                result.Success = false;
                result.Message = permsValidation.Message;
                return result;
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Test 1: List blobs (verify container access)
            result.AddDetail("Testing container access (list blobs)...");
            var listUri = containerUriWithSas + "&restype=container&comp=list&maxresults=1";
            var listResponse = await httpClient.GetAsync(listUri);
            
            if (!listResponse.IsSuccessStatusCode)
            {
                var errorContent = await listResponse.Content.ReadAsStringAsync();
                _logger.Error("Container list failed: {Status} {Error}", listResponse.StatusCode, errorContent);
                result.Success = false;
                result.Message = $"Failed to access container: {listResponse.StatusCode}";
                result.AddDetail($"Error: {errorContent}");
                return result;
            }
            
            result.AddDetail("✓ Successfully accessed container");

            // Test 2: Write test blob
            var testBlobName = $"{config.SanitizedNIT}/_healthcheck/{Guid.NewGuid()}.txt";
            var testContent = $"Health check from BackupConfigurator at {DateTime.UtcNow:O}";
            var blobUri = $"{config.AzureContainerUrl.TrimEnd('/')}/{testBlobName}?{config.AzureSasToken}";

            result.AddDetail($"Testing blob write: {testBlobName}...");
            
            using var content = new StringContent(testContent, System.Text.Encoding.UTF8, "text/plain");
            content.Headers.Add("x-ms-blob-type", "BlockBlob");
            
            var putResponse = await httpClient.PutAsync(blobUri, content);
            
            if (!putResponse.IsSuccessStatusCode)
            {
                var putError = await putResponse.Content.ReadAsStringAsync();
                _logger.Error("Blob write failed: {Status} {Error}", putResponse.StatusCode, putError);
                result.Success = false;
                result.Message = $"Failed to write test blob: {putResponse.StatusCode}";
                result.AddDetail($"Error: {putError}");
                return result;
            }
            
            result.AddDetail($"✓ Successfully wrote test blob: {testBlobName}");

            // Test 3: Delete test blob
            result.AddDetail("Testing blob delete...");
            var deleteResponse = await httpClient.DeleteAsync(blobUri);
            
            if (!deleteResponse.IsSuccessStatusCode)
            {
                var deleteError = await deleteResponse.Content.ReadAsStringAsync();
                _logger.Warning("Blob delete failed: {Status} {Error}", deleteResponse.StatusCode, deleteError);
            }
            else
            {
                result.AddDetail("✓ Successfully deleted test blob");
            }

            result.Success = true;
            result.Message = "Azure Blob Storage connection test successful";
            _logger.Information("Azure connection test successful");
            
            return result;
        }
        catch (UriFormatException ex)
        {
            _logger.Error(ex, "Invalid Azure Container URL format");
            result.Success = false;
            result.Message = $"Invalid container URL format: {ex.Message}";
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP request error during Azure test");
            result.Success = false;
            result.Message = $"Network error: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during Azure connection test");
            result.Success = false;
            result.Message = $"Unexpected error: {ex.Message}";
            return result;
        }
    }

    public static string BuildContainerUri(BackupConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.AzureContainerUrl))
            throw new UriFormatException("AzureContainerUrl is empty");

        var trimmedUrl = config.AzureContainerUrl.Trim();

        // If user pasted the full container URL already including the SAS (contains '?'),
        // return it as-is to avoid appending the SAS twice and invalidating the signature.
        if (trimmedUrl.Contains('?'))
        {
            return trimmedUrl;
        }

        var baseUrl = trimmedUrl.TrimEnd('/');
        var sasToken = config.NormalizedSasToken;

        return $"{baseUrl}{sasToken}";
    }

    private static string RedactSasSignature(string uri)
    {
        try
        {
            var u = new Uri(uri);
            var basePath = uri.Split('?')[0];
            var query = u.Query;
            if (string.IsNullOrEmpty(query))
                return basePath;

            var q = query.TrimStart('?').Split('&');
            for (int i = 0; i < q.Length; i++)
            {
                if (q[i].StartsWith("sig=", StringComparison.OrdinalIgnoreCase))
                {
                    q[i] = "sig=REDACTED";
                }
            }
            var redactedQuery = string.Join("&", q);
            return basePath + "?" + redactedQuery;
        }
        catch
        {
            var idx = uri.IndexOf("sig=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return uri;
            var start = idx;
            var end = uri.IndexOf('&', start);
            if (end < 0) end = uri.Length;
            return uri.Substring(0, start) + "sig=REDACTED" + (end < uri.Length ? uri.Substring(end) : string.Empty);
        }
    }

    private static ValidationResult ValidateSasSignature(string uri)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(uri))
                return ValidationResult.Fail("SAS URI is empty");

            var u = new Uri(uri);
            var query = u.Query;
            if (string.IsNullOrEmpty(query))
                return ValidationResult.Fail("SAS token missing in the URL. Paste full URL including the SAS.");

            var q = query.TrimStart('?').Split('&');
            string sigValue = null!;
            foreach (var part in q)
            {
                if (part.StartsWith("sig=", StringComparison.OrdinalIgnoreCase))
                {
                    sigValue = part.Substring(4);
                    break;
                }
            }

            if (string.IsNullOrEmpty(sigValue))
                return ValidationResult.Fail("SAS signature (sig) not found in URL");

            // URL-decode the signature
            var decoded = Uri.UnescapeDataString(sigValue);

            // Try Base64 decode
            try
            {
                var bytes = Convert.FromBase64String(decoded);
                if (bytes.Length == 0)
                {
                    return ValidationResult.Fail("SAS signature decoded to empty bytes — signature invalid");
                }
            }
            catch (FormatException)
            {
                return ValidationResult.Fail("SAS signature appears malformed or not valid Base64. Regenerate SAS via Azure Portal and paste full URL.");
            }

            return ValidationResult.Ok("SAS signature looks valid (Base64) — proceeding");
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Failed to validate SAS signature: {ex.Message}");
        }
    }

    private static ValidationResult ValidateSasPermissionsAndTimes(string uri)
    {
        try
        {
            var u = new Uri(uri);
            var query = u.Query;
            if (string.IsNullOrEmpty(query))
                return ValidationResult.Fail("SAS token missing in the URL. Paste full URL including the SAS.");

            var parts = query.TrimStart('?').Split('&');
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parts)
            {
                var idx = p.IndexOf('=');
                if (idx > 0)
                {
                    var key = p.Substring(0, idx);
                    var val = p.Substring(idx + 1);
                    dict[key] = Uri.UnescapeDataString(val);
                }
            }

            // Check permissions
            if (!dict.TryGetValue("sp", out var spValue) || string.IsNullOrWhiteSpace(spValue))
            {
                return ValidationResult.Fail("SAS permissions (sp) not found. Regenerate SAS with permissions: r,c,w,d,l");
            }

            var required = new[] { 'r', 'c', 'w', 'd', 'l' };
            var missing = required.Where(r => !spValue.Contains(r)).ToArray();
            if (missing.Length > 0)
            {
                return ValidationResult.Fail($"SAS permissions missing required flags: {string.Join(',', missing)}. Regenerate SAS with permissions r,c,w,d,l");
            }

            // Check start and expiry times
            var now = DateTimeOffset.UtcNow;
            if (dict.TryGetValue("st", out var stVal) && !string.IsNullOrWhiteSpace(stVal))
            {
                if (DateTimeOffset.TryParse(stVal, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var st))
                {
                    if (st > now.AddMinutes(5))
                        return ValidationResult.Fail($"SAS start time (st) is in the future: {st:o}. Ensure start is not in the future or remove st.");
                }
            }

            if (dict.TryGetValue("se", out var seVal) && !string.IsNullOrWhiteSpace(seVal))
            {
                if (DateTimeOffset.TryParse(seVal, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var se))
                {
                    if (se <= now)
                        return ValidationResult.Fail($"SAS has already expired (se={se:o}). Regenerate SAS with a valid expiry.");
                }
            }

            // Check Signed IP
            if (dict.TryGetValue("sip", out var sipVal) && !string.IsNullOrWhiteSpace(sipVal))
            {
                return ValidationResult.Fail($"SAS has an IP restriction (sip={sipVal}). Ensure your client IP is allowed or generate a SAS without IP restriction.");
            }

            return ValidationResult.Ok("SAS permissions and time window look OK");
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Failed to validate SAS parameters: {ex.Message}");
        }
    }
}
