namespace FTG.API.Auth;

using System.Security.Cryptography;
using System.Text;
using Core.Logging;

public interface IAuth
{
    bool IsAuthenticated(HttpRequest request);
}
public class Auth(IConfiguration configuration) : IAuth
{
    public bool IsAuthenticated(HttpRequest request)
    {
        // Get the shared key from configuration
        var expectedKey = configuration["Authentication:SharedKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            GlobalLogger.LogWarning("Shared key not configured");
            return false;
        }

        // Check for Authorization header
        if (!request.Headers.TryGetValue("Authorization", out var value))
        {
            return false;
        }

        var authHeader = value.FirstOrDefault();

        // Handle both "Bearer token" and direct token formats
        var providedKey = authHeader?.StartsWith("Bearer ") == true
            ? authHeader[7..]
            : authHeader;

        // Use secure string comparison to prevent timing attacks
        return SecureStringCompare(expectedKey, providedKey);
    }

    private static bool SecureStringCompare(string? expected, string? provided)
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