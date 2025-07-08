using System.Collections.ObjectModel;

namespace FTG.Debug.Email;

public partial class MainPage : ContentPage
{
    private readonly MailgunService _mailgunService;
    private readonly ConfigurationService _configService;
    private ObservableCollection<EmailAttachment> Attachments { get; set; }

    public MainPage()
    {
        InitializeComponent();
        _mailgunService = new MailgunService();
        _configService = new ConfigurationService();
        Attachments = new ObservableCollection<EmailAttachment>();
        
        // Set binding context for attachments
        BindingContext = this;
        
        // Set default values
        DeliveryDatePicker.Date = DateTime.Now;
        DeliveryTimePicker.Time = DateTime.Now.TimeOfDay;
        
        // Load settings
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _configService.GetMailgunSettingsAsync();
            
            // Pre-populate fields with saved settings
            if (!string.IsNullOrWhiteSpace(settings.ApiKey) && 
                settings.ApiKey != "your-mailgun-api-key-here" &&
                settings.ApiKey != "your-actual-mailgun-api-key-here")
            {
                ApiKeyEntry.Text = settings.ApiKey;
            }
            
            DomainEntry.Text = settings.Domain;
            FromEntry.Text = settings.DefaultFrom;
            
            // Set some sample data for testing
            SubjectEntry.Text = "Processed FTG Manifest For Scale - ___ORIGINAL_FILENAME___ - Complete @ ___PROCESS_DATETIME___";
            HtmlBodyEditor.Text = @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
	<h2 style=""color: #28a745;"">Fresh To Go Manifest PDF Processing Complete! ✅</h2>

	<p>The provided Fresh to Go Manifest PDF has been successfully processed and converted into the required formats for Scale Interfacing.</p>
	<h3 style=""margin-top: 0;"">Manifest Date: ___MANIFEST_DATE___ </h3>

	<div style=""background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;"">
		<h3 style=""margin-top: 0;"">Processing Details:</h3>
		<ul style=""list-style-type: none; padding-left: 0;"">
			<li>📄 <strong>Original File:</strong> ___ORIGINAL_FILENAME___</li>
			<li>📅 <strong>Processed At:</strong> ___PROCESS_DATETIME___</li>
		</ul>
	</div>

	<div style=""background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0;"">
		<h3 style=""margin-top: 0;"">Attached Files:</h3>
		<ol>
            <li><strong>___FileNAME___</strong> - Manhattan Scale Receipt RCXML format</li>
			<li><strong>___FileNAME___</strong> - Manhattan Scale Shipment SHXML format</li>
		</ol>
	</div>

</div>";
            
            // Check if we have a valid API key
            var hasValidKey = await _configService.HasValidApiKeyAsync();
            if (!hasValidKey)
            {
                StatusLabel.Text = "⚠️ Please enter your Mailgun API Key";
                StatusLabel.TextColor = Colors.Orange;
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error loading settings: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(ApiKeyEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the Mailgun API Key", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(DomainEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the Mailgun domain", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(FromEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the sender email", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ToEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the recipient email", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SubjectEntry.Text))
            {
                await DisplayAlert("Error", "Please enter the email subject", "OK");
                return;
            }

            // Update UI
            SendEmailButton.IsEnabled = false;
            StatusLabel.Text = "Sending email...";
            StatusLabel.TextColor = Colors.Orange;

            // Prepare email data
            var emailData = new MailgunEmailData
            {
                ApiKey = ApiKeyEntry.Text,
                Domain = DomainEntry.Text,
                From = FromEntry.Text,
                To = ToEntry.Text,
                Subject = SubjectEntry.Text,
                HtmlBody = HtmlBodyEditor.Text,
                SkipEmailSend = SkipEmailSendCheckBox.IsChecked,
                Attachments = Attachments.ToList()
            };

            // Set delivery time if scheduled
            if (ScheduleDeliveryCheckBox.IsChecked)
            {
                var deliveryDate = DeliveryDatePicker.Date.Add(DeliveryTimePicker.Time);
                emailData.DeliveryTime = deliveryDate;
            }

            // Send email
            var result = await _mailgunService.SendEmailAsync(emailData);

            // Save API key if email was successful (for future use)
            if (result.Success && !string.IsNullOrWhiteSpace(emailData.ApiKey))
            {
                await _configService.SaveApiKeyAsync(emailData.ApiKey);
            }

            // Update UI based on result
            if (result.Success)
            {
                StatusLabel.Text = $"✅ {result.Message}";
                StatusLabel.TextColor = Colors.Green;
                await DisplayAlert("Success", result.Message, "OK");
                
                // Clear form after successful send
                ClearForm();
            }
            else
            {
                StatusLabel.Text = $"❌ {result.Message}";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Error", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SendEmailButton.IsEnabled = true;
        }
    }

    private async void OnAddAttachmentClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.data" } }, // Generic data type
                    { DevicePlatform.Android, new[] { "*/*" } }, // All file types
                    { DevicePlatform.WinUI, new[] { "*" } }, // All file types
                    { DevicePlatform.macOS, new[] { "public.data" } }, // Generic data type
                });

            var options = new PickOptions()
            {
                PickerTitle = "Select file to attach",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                // Read file content
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                // Create attachment
                var attachment = new EmailAttachment
                {
                    FileName = result.FileName,
                    Content = fileContent,
                    ContentType = result.ContentType ?? "application/octet-stream"
                };

                Attachments.Add(attachment);
                
                StatusLabel.Text = $"Added attachment: {result.FileName}";
                StatusLabel.TextColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add attachment: {ex.Message}", "OK");
        }
    }

    private void OnRemoveAttachmentClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EmailAttachment attachment)
        {
            Attachments.Remove(attachment);
            StatusLabel.Text = $"Removed attachment: {attachment.FileName}";
            StatusLabel.TextColor = Colors.Orange;
        }
    }

    private void ClearForm()
    {
        ToEntry.Text = string.Empty;
        SubjectEntry.Text = string.Empty;
        HtmlBodyEditor.Text = string.Empty;
        ScheduleDeliveryCheckBox.IsChecked = false;
        SkipEmailSendCheckBox.IsChecked = false;
        DeliveryDatePicker.Date = DateTime.Now.AddDays(1);
        DeliveryTimePicker.Time = new TimeSpan(10, 0, 0);
        Attachments.Clear();
    }
}