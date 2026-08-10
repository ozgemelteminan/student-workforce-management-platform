using System.Security.Cryptography;
using StudentWorkforceManagement.Application.Common.Storage;

namespace StudentWorkforceManagement.Infrastructure.Storage;

public static class StorageKeyFactory
{
    public static string Create(UploadTargetRequest request)
    {
        var scope = NormalizeSegment(request.OwnershipScope);
        var extension = NormalizeExtension(request.FileExtension);
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        return $"{scope}/{DateTimeOffset.UtcNow:yyyy/MM/dd}/{random}{extension}";
    }

    private static string NormalizeSegment(string value)
    {
        var safe = new string(value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) || ch is '-' ? ch : '-').ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "files" : safe;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (normalized.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return string.Empty;
        }

        return $".{normalized}";
    }
}
