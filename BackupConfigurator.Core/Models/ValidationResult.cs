using System.Diagnostics;

namespace BackupConfigurator.Core.Models;

public class ValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();

    public static ValidationResult Ok(string message = "Success")
    {
        return new ValidationResult { Success = true, Message = message };
    }

    public static ValidationResult Fail(string message, List<string>? details = null)
    {
        return new ValidationResult
        {
            Success = false,
            Message = message,
            Details = details ?? new List<string>()
        };
    }

    public void AddDetail(string detail)
    {
        Debug.WriteLine($"Validation Detail: {detail}");
        Details.Add(detail);
    }

    public override string ToString()
    {
        var result = $"{(Success ? "✓" : "✗")} {Message}";
        if (Details.Any())
        {
            result += "\n" + string.Join("\n", Details.Select(d => $"  • {d}"));
        }
        return result;
    }
}
