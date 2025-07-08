# Mailgun MAUI Email Sender Setup

## Initial Setup

1. **Clone the repository**
2. **Copy the example configuration:**
   ```bash
   cp appsettings.example.json appsettings.json
   ```
3. **Edit `appsettings.json` with your actual Mailgun settings:**
   ```json
   {
     "MailgunSettings": {
       "ApiKey": "your-actual-mailgun-api-key-here",
       "Domain": "your-domain.com",
       "DefaultFrom": "noreply@your-domain.com"
     }
   }
   ```

## Security Notes

- **`appsettings.json`** is ignored by git and contains your real API key
- **`appsettings.example.json`** is committed to git as a template
- The app automatically saves valid API keys locally for future use
- API keys are stored in the app's private data directory

## Alternative Methods

### Environment Variables (Development)
You can also set environment variables:
```bash
export MAILGUN_API_KEY="your-api-key"
export MAILGUN_DOMAIN="your-domain.com"
```

### User Secrets (Visual Studio)
For Visual Studio users, you can use the Secret Manager:
1. Right-click project → Manage User Secrets
2. Add your configuration:
   ```json
   {
     "MailgunSettings:ApiKey": "your-api-key"
   }
   ```

## First Run

On first run, if no API key is found:
1. The app will show a warning message
2. Enter your API key in the form
3. Send a test email successfully
4. The API key will be automatically saved for future use

## Team Setup

For team development:
1. Each developer copies `appsettings.example.json` to `appsettings.json`
2. Each developer adds their own API key to their local `appsettings.json`
3. The actual API keys never get committed to git