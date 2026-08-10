using System.Text;
using System.Text.Encodings.Web;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email;

public sealed class EmailTemplateRenderer
{
    public string RenderHtml(EmailMessage message)
    {
        var builder = new StringBuilder();
        builder.Append("<html><body>");
        builder.Append("<h1>").Append(HtmlEncoder.Default.Encode(message.Subject)).Append("</h1>");
        builder.Append("<p>Template: ").Append(HtmlEncoder.Default.Encode(message.TemplateKey)).Append("</p>");
        var templateData = new Dictionary<string, string>(message.TemplateData, StringComparer.Ordinal);
        foreach (var secret in message.SecretTemplateData ?? new Dictionary<string, string>())
        {
            templateData[secret.Key] = secret.Value;
        }

        if (templateData.Count > 0)
        {
            builder.Append("<dl>");
            foreach (var item in templateData.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                builder.Append("<dt>").Append(HtmlEncoder.Default.Encode(item.Key)).Append("</dt>");
                builder.Append("<dd>").Append(HtmlEncoder.Default.Encode(item.Value)).Append("</dd>");
            }
            builder.Append("</dl>");
        }
        builder.Append("</body></html>");
        return builder.ToString();
    }
}
