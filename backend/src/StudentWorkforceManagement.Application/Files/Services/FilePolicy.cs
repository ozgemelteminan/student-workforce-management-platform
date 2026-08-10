using StudentWorkforceManagement.Application.Common.Exceptions;

namespace StudentWorkforceManagement.Application.Files.Services;

public static class FilePolicy
{
    public const long MaxFileSizeBytes = 1024L * 1024L * 1024L;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".csv", ".txt", ".zip", ".png", ".jpg", ".jpeg", ".svg", ".webp", ".mp4", ".mov"
    };

    public static string NormalizeExtension(string extension)
    {
        var normalized = extension.Trim().StartsWith('.') ? extension.Trim().ToLowerInvariant() : $".{extension.Trim().ToLowerInvariant()}";
        if (!AllowedExtensions.Contains(normalized))
        {
            throw new ConflictException("File extension is not allowed.");
        }
        return normalized;
    }

    public static void ValidateSize(long size)
    {
        if (size <= 0 || size > MaxFileSizeBytes)
        {
            throw new ConflictException("File size must be between 1 byte and 1 GB.");
        }
    }
}
