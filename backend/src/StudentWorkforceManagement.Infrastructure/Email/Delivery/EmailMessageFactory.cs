using System.Text.Json;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Email.Delivery;

public sealed class EmailMessageFactory(IEmailSecretProtector secretProtector)
{
    private const string SecretKeyPrefix = "__protected:";

    public EmailMessage FromDelivery(EmailDelivery delivery)
    {
        var serializedData = string.IsNullOrWhiteSpace(delivery.TemplateDataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(delivery.TemplateDataJson) ?? new Dictionary<string, string>();

        var templateData = new Dictionary<string, string>(StringComparer.Ordinal);
        var secretTemplateData = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in serializedData)
        {
            if (item.Key.StartsWith(SecretKeyPrefix, StringComparison.Ordinal))
            {
                secretTemplateData[item.Key[SecretKeyPrefix.Length..]] = secretProtector.Unprotect(item.Value);
                continue;
            }

            templateData[item.Key] = item.Value;
        }

        return new EmailMessage(delivery.RecipientEmail, delivery.Subject, delivery.TemplateKey, templateData, delivery.IdempotencyKey, secretTemplateData);
    }

    public static string ProtectedTemplateDataKey(string key) => $"{SecretKeyPrefix}{key}";

    public static string RemoveProtectedTemplateData(string? templateDataJson)
    {
        if (string.IsNullOrWhiteSpace(templateDataJson))
        {
            return "{}";
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(templateDataJson) ?? new Dictionary<string, string>();
        var publicData = data
            .Where(item => item.Key.StartsWith(SecretKeyPrefix, StringComparison.Ordinal) == false)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        return JsonSerializer.Serialize(publicData);
    }
}
