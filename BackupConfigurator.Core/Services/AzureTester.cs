using Azure;
using Azure.Storage.Blobs;
using BackupConfigurator.Core.Models;
using Serilog;

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

            // Normalize and build the full URI with SAS token
            var containerUriWithSas = BuildContainerUri(config);

            result.AddDetail($"Testing connection to: {config.AzureContainerUrl}");

            // Create BlobContainerClient with SAS URI
            var containerClient = new BlobContainerClient(new Uri(containerUriWithSas));

            // Test 1: Get container properties
            try
            {
                var properties = await containerClient.GetPropertiesAsync();
                result.AddDetail("✓ Successfully accessed container properties");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return ValidationResult.Fail("Container not found. Please verify the container URL.");
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                return ValidationResult.Fail("Access denied. Please verify the SAS token has proper permissions.");
            }

            // Test 2: Write test blob
            var testBlobName = $"{config.SanitizedNIT}/_healthcheck/{Guid.NewGuid()}.txt";
            var testContent = $"Health check from BackupConfigurator at {DateTime.UtcNow:O}";

            try
            {
                var blobClient = containerClient.GetBlobClient(testBlobName);
                using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
                await blobClient.UploadAsync(ms, overwrite: true);
                result.AddDetail($"✓ Successfully wrote test blob: {testBlobName}");

                // Test 3: Delete test blob
                await blobClient.DeleteAsync();
                result.AddDetail("✓ Successfully deleted test blob");
            }
            catch (RequestFailedException ex)
            {
                return ValidationResult.Fail($"Failed to write/delete test blob: {ex.Message}. Ensure SAS token has write and delete permissions.");
            }

            result.Success = true;
            result.Message = "Azure Blob Storage connection test successful";

            _logger.Information("Azure connection test successful");
            return result;
        }
        catch (UriFormatException ex)
        {
            _logger.Error(ex, "Invalid Azure Container URL format");
            return ValidationResult.Fail($"Invalid container URL format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during Azure connection test");
            return ValidationResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    public static string BuildContainerUri(BackupConfiguration config)
    {
        var baseUrl = config.AzureContainerUrl.TrimEnd('/');
        var sasToken = config.NormalizedSasToken;

        return $"{baseUrl}{sasToken}";
    }
}
