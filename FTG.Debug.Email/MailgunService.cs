namespace FTG.Debug.Email;

using System.Text;
using System.Net.Http;
using System.Globalization;
using System.ComponentModel;

public class MailgunService
{
    private readonly HttpClient _httpClient;

    public MailgunService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<MailgunResponse> SendEmailAsync(MailgunEmailData emailData)
    {
        try
        {
            // Debug mode - skip actual email sending
            if (emailData.SkipEmailSend)
            {
                LogDebug($"Skipping email send as SkipEmailSend is set to true. Email would be sent to: {emailData.To}");
                return new MailgunResponse 
                { 
                    Success = true, 
                    Message = "Email sent successfully (Debug Mode - No actual email sent)" 
                };
            }

            // Prepare form data
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(emailData.From), "from");
            formData.Add(new StringContent(emailData.To), "to");
            formData.Add(new StringContent(emailData.Subject), "subject");
            formData.Add(new StringContent(emailData.HtmlBody), "html");

            // Add delivery time if scheduled
            if (emailData.DeliveryTime.HasValue)
            {
                var deliveryTimeRfc2822 = FormatDateTimeRfc2822(emailData.DeliveryTime.Value);
                formData.Add(new StringContent(deliveryTimeRfc2822), "o:deliverytime");
                LogDebug($"Email scheduled for delivery at: {deliveryTimeRfc2822}");
            }
            else
            {
                LogDebug("Email will be sent immediately");
            }

            // Add attachments if any
            if (emailData.Attachments != null && emailData.Attachments.Any())
            {
                LogDebug($"Adding {emailData.Attachments.Count} attachments");
                foreach (var attachment in emailData.Attachments)
                {
                    var fileContent = new ByteArrayContent(attachment.Content);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType);
                    formData.Add(fileContent, "attachment", attachment.FileName);
                    LogDebug($"Added attachment: {attachment.FileName} ({attachment.SizeText})");
                }
            }

            // Prepare authorization header
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{emailData.ApiKey}"));
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

            // Send request
            var url = $"https://api.mailgun.net/v3/{emailData.Domain}/messages";
            LogDebug($"Sending email to URL: {url}");
            
            var response = await _httpClient.PostAsync(url, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            LogDebug($"Mailgun server reply: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                return new MailgunResponse 
                { 
                    Success = true, 
                    Message = "Email sent successfully" 
                };
            }
            else
            {
                return new MailgunResponse 
                { 
                    Success = false, 
                    Message = $"Failed to send email. Status: {response.StatusCode}, Response: {responseContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Exception occurred: {ex.Message}");
            return new MailgunResponse 
            { 
                Success = false, 
                Message = $"Failed to send email with error: {ex.Message}" 
            };
        }
    }

    private string FormatDateTimeRfc2822(DateTime dateTime)
    {
        // Convert to UTC if not already
        if (dateTime.Kind == DateTimeKind.Local)
        {
            dateTime = dateTime.ToUniversalTime();
        }

        // Format as RFC 2822 (e.g., "Fri, 14 Jul 2023 10:00:00 +0000")
        return dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss +0000", CultureInfo.InvariantCulture);
    }

    private void LogDebug(string message)
    {
        // In a real application, you might want to use a proper logging framework
        System.Diagnostics.Debug.WriteLine($"[MailgunService] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        Console.WriteLine($"[MailgunService] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public class MailgunEmailData
{
    public string ApiKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime? DeliveryTime { get; set; }
    public bool SkipEmailSend { get; set; } = false;
    public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}

public class EmailAttachment : INotifyPropertyChanged
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    
    public string SizeText
    {
        get
        {
            if (Content == null) return "0 bytes";
            
            double bytes = Content.Length;
            string[] suffixes = { "bytes", "KB", "MB", "GB" };
            int counter = 0;
            
            while (bytes >= 1024 && counter < suffixes.Length - 1)
            {
                bytes /= 1024;
                counter++;
            }
            
            return $"{bytes:0.##} {suffixes[counter]}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class MailgunResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}