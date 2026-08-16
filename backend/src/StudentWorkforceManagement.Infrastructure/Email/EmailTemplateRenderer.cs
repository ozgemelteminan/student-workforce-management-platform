using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email;

public sealed class EmailTemplateRenderer(IOptions<EmailOptions>? options = null)
{
    private readonly EmailOptions _options = options?.Value ?? new EmailOptions();
    
    public string RenderHtml(EmailMessage message)
    {
        if (IsInvitationTemplate(message.TemplateKey))
        {
            return RenderInvitationHtml(message);
        }

        return RenderDefaultHtml(message);
    }

    private string RenderInvitationHtml(EmailMessage message)
    {
        var token = GetTemplateValue(message, "invitationToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Invitation email requires an invitationToken.");
        }

        var frontendBaseUrl = _options.FrontendBaseUrl.TrimEnd('/');

        var acceptUrl =
            $"{frontendBaseUrl}/invitations/accept?token={Uri.EscapeDataString(token)}";

        var encodedAcceptUrl = HtmlEncoder.Default.Encode(acceptUrl);

        var isResend = string.Equals(
            message.TemplateKey,
            "auth.invitation.resend",
            StringComparison.Ordinal);

        var eyebrow = isResend
            ? "🔄 Fresh invitation"
            : "🎉 You're invited!";

        var heading = isResend
            ? "Your invitation is ready again ✨"
            : "Welcome to Student Workforce Management ✨";

        var description = isResend
            ? "We refreshed your invitation, so you can continue setting up your account."
            : "You've been invited to join your department's Student Workforce Management workspace.";

        var expirationText = GetExpirationText(message);

        var builder = new StringBuilder();

        builder.Append("""
<!doctype html>
<html>
<body style="margin:0;padding:0;background:#F7F4EF;font-family:Arial,Helvetica,sans-serif;color:#242424;">
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#F7F4EF;padding:32px 16px;">
<tr>
<td align="center">

<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
       style="max-width:560px;background:#FFFFFF;border:1px solid #E2DDD6;border-radius:18px;overflow:hidden;">

<tr>
<td style="padding:36px 36px 12px 36px;text-align:center;">

<div style="font-size:42px;line-height:1;margin-bottom:18px;">
🌟
</div>

<div style="font-size:13px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;color:#C91F28;margin-bottom:12px;">
""");

        builder.Append(HtmlEncoder.Default.Encode(eyebrow));

        builder.Append("""
</div>

<h1 style="margin:0;font-size:28px;line-height:1.25;color:#242424;">
""");

        builder.Append(HtmlEncoder.Default.Encode(heading));

        builder.Append("""
</h1>

</td>
</tr>

<tr>
<td style="padding:18px 36px 0 36px;">

<p style="margin:0 0 16px 0;font-size:16px;line-height:1.65;color:#242424;">
Hi there 👋
</p>

<p style="margin:0 0 24px 0;font-size:16px;line-height:1.65;color:#66615C;">
""");

        builder.Append(HtmlEncoder.Default.Encode(description));

        builder.Append("""
</p>

</td>
</tr>

<tr>
<td align="center" style="padding:6px 36px 28px 36px;">

<a href="
""");

        builder.Append(encodedAcceptUrl);

        builder.Append("""
" style="display:inline-block;background:#C91F28;color:#FFFFFF;text-decoration:none;font-size:16px;font-weight:700;padding:14px 26px;border-radius:10px;">
✨ Accept invitation
</a>

</td>
</tr>
""");

        if (!string.IsNullOrWhiteSpace(expirationText))
        {
            builder.Append("""
<tr>
<td style="padding:0 36px 24px 36px;">

<div style="background:#F7F4EF;border-radius:10px;padding:14px 16px;font-size:14px;line-height:1.5;color:#66615C;text-align:center;">
⏰ This invitation expires on
<strong style="color:#242424;">
""");

            builder.Append(HtmlEncoder.Default.Encode(expirationText));

            builder.Append("""
</strong>.
</div>

</td>
</tr>
""");
        }

        builder.Append("""
<tr>
<td style="padding:0 36px 28px 36px;">

<p style="margin:0 0 8px 0;font-size:13px;line-height:1.5;color:#66615C;">
If the button doesn't work, copy and paste this link into your browser:
</p>

<p style="margin:0;word-break:break-all;font-size:12px;line-height:1.5;">
<a href="
""");

        builder.Append(encodedAcceptUrl);

        builder.Append("""
" style="color:#C91F28;text-decoration:underline;">
""");

        builder.Append(encodedAcceptUrl);

        builder.Append("""
</a>
</p>

</td>
</tr>

<tr>
<td style="padding:24px 36px 32px 36px;border-top:1px solid #E2DDD6;text-align:center;">

<p style="margin:0 0 6px 0;font-size:14px;color:#242424;font-weight:600;">
See you soon 🌟
</p>

<p style="margin:0;font-size:12px;color:#66615C;">
Student Workforce Management
</p>

</td>
</tr>

</table>

<p style="margin:18px 0 0 0;font-size:11px;color:#8A857F;text-align:center;">
This invitation was sent automatically. If you weren't expecting it, you can safely ignore this email.
</p>

</td>
</tr>
</table>
</body>
</html>
""");

        return builder.ToString();
    }

    private static string RenderDefaultHtml(EmailMessage message)
    {
        var builder = new StringBuilder();

        builder.Append("<html><body>");
        builder.Append("<h1>")
            .Append(HtmlEncoder.Default.Encode(message.Subject))
            .Append("</h1>");

        builder.Append("<p>Template: ")
            .Append(HtmlEncoder.Default.Encode(message.TemplateKey))
            .Append("</p>");

        var templateData =
            new Dictionary<string, string>(
                message.TemplateData,
                StringComparer.Ordinal);

        foreach (var secret in
                 message.SecretTemplateData ??
                 new Dictionary<string, string>())
        {
            templateData[secret.Key] = secret.Value;
        }

        if (templateData.Count > 0)
        {
            builder.Append("<dl>");

            foreach (var item in templateData.OrderBy(
                         item => item.Key,
                         StringComparer.Ordinal))
            {
                builder.Append("<dt>")
                    .Append(HtmlEncoder.Default.Encode(item.Key))
                    .Append("</dt>");

                builder.Append("<dd>")
                    .Append(HtmlEncoder.Default.Encode(item.Value))
                    .Append("</dd>");
            }

            builder.Append("</dl>");
        }

        builder.Append("</body></html>");

        return builder.ToString();
    }

    private static bool IsInvitationTemplate(string templateKey)
    {
        return templateKey is
            "auth.invitation.student"
            or "auth.invitation.user"
            or "auth.invitation.resend";
    }

    private static string? GetTemplateValue(
        EmailMessage message,
        string key)
    {
        if (message.SecretTemplateData is not null &&
            message.SecretTemplateData.TryGetValue(key, out var secretValue))
        {
            return secretValue;
        }

        return message.TemplateData.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static string? GetExpirationText(EmailMessage message)
    {
        var rawExpiration = GetTemplateValue(message, "expiresAt");

        if (string.IsNullOrWhiteSpace(rawExpiration) ||
            !DateTimeOffset.TryParse(
                rawExpiration,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiration))
        {
            return null;
        }

        try
        {
            var timezone =
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

            var localExpiration =
                TimeZoneInfo.ConvertTime(expiration, timezone);

            return localExpiration.ToString(
                "dd MMMM yyyy, HH:mm",
                CultureInfo.InvariantCulture) + " · Europe/Istanbul";
        }
        catch (TimeZoneNotFoundException)
        {
            return expiration.ToString(
                "dd MMMM yyyy, HH:mm 'UTC'",
                CultureInfo.InvariantCulture);
        }
        catch (InvalidTimeZoneException)
        {
            return expiration.ToString(
                "dd MMMM yyyy, HH:mm 'UTC'",
                CultureInfo.InvariantCulture);
        }
    }
}