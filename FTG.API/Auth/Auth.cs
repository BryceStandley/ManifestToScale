namespace FTG.API.Auth;

using System.Security.Cryptography;
using System.Text;
using FTG.Core.Logging;

public interface IAuth
{
    bool IsAuthenticated(HttpRequest request);
}
public class Auth(IConfiguration configuration, IWebHostEnvironment environment) : IAuth
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _environment = environment;
    
    public bool IsAuthenticated(HttpRequest request)
    {
        // Get the shared key from configuration
        var expectedKey = _configuration["Authentication:SharedKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            GlobalLogger.LogWarning("Shared key not configured");
            return false;
        }

        // Check for Authorization header
        if (!request.Headers.ContainsKey("Authorization"))
        {
            return false;
        }

        var authHeader = request.Headers["Authorization"].FirstOrDefault();

        // Handle both "Bearer token" and direct token formats
        var providedKey = authHeader?.StartsWith("Bearer ") == true
            ? authHeader.Substring(7)
            : authHeader;

        // Use secure string comparison to prevent timing attacks
        return SecureStringCompare(expectedKey, providedKey);
    }

    private bool SecureStringCompare(string? expected, string? provided)
    {
        if (expected == null || provided == null)
            return false;

        if (expected.Length != provided.Length)
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}