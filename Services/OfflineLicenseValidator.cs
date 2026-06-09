using LicensingAPI.Models.Licenses;
using NSec.Cryptography;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OfflineLicenseValidator
{
    private readonly string _publicKey;

    public OfflineLicenseValidator(string publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentNullException(nameof(publicKey), "Public key cannot be null or empty.");

        _publicKey = publicKey;
    }

    public OfflineValidationResult Validate(string licenseKey)
    {
        try
        {
            // ── Step 1: Basic format check ───────────────────────────
            if (string.IsNullOrWhiteSpace(licenseKey))
                return OfflineValidationResult.Fail("License key cannot be empty.");

            if (!licenseKey.StartsWith("key/"))
                return OfflineValidationResult.Fail(
                    "Invalid key format. Offline keys must start with 'key/'.");

            // ── Step 2: Split into data and signature ────────────────
            // Format: key/{BASE64URL_DATA}.{BASE64URL_SIGNATURE}
            var withoutPrefix = licenseKey.Substring(4);  // remove "key/"
            var dotIndex = withoutPrefix.LastIndexOf('.');

            if (dotIndex < 0)
                return OfflineValidationResult.Fail(
                    "Invalid key structure. Expected 'key/{data}.{signature}'.");

            var encodedData = withoutPrefix.Substring(0, dotIndex);
            var encodedSignature = withoutPrefix.Substring(dotIndex + 1);

            if (string.IsNullOrWhiteSpace(encodedData) ||
                string.IsNullOrWhiteSpace(encodedSignature))
                return OfflineValidationResult.Fail("Key data or signature is missing.");

            // ── Step 3: Verify Ed25519 signature ─────────────────────
            // Signing data is the full "key/{encodedData}" string as bytes
            var signingData = Encoding.UTF8.GetBytes($"key/{encodedData}");
            var signatureBytes = Base64UrlDecode(encodedSignature);
            var publicKeyBytes = Convert.FromHexString(_publicKey);

            var algorithm = SignatureAlgorithm.Ed25519;
            var pubKey = NSec.Cryptography.PublicKey.Import(
                algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey);

            bool isSignatureValid = algorithm.Verify(pubKey, signingData, signatureBytes);

            if (!isSignatureValid)
                return OfflineValidationResult.Fail(
                    "License signature verification failed. Key may be tampered.");

            // ── Step 4: Decode payload ───────────────────────────────
            var payloadBytes = Base64UrlDecode(encodedData);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);

            var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null)
                return OfflineValidationResult.Fail("Failed to parse license payload.");

            // ── Step 5: Check expiry ─────────────────────────────────
            if (payload.Expiry.HasValue && payload.Expiry.Value < DateTime.UtcNow)
                return OfflineValidationResult.Fail(
                    $"License expired on {payload.Expiry.Value:yyyy-MM-dd}.");

            return OfflineValidationResult.Success(payload);
        }
        catch (FormatException)
        {
            return OfflineValidationResult.Fail(
                "License key contains invalid Base64 encoding.");
        }
        catch (Exception ex)
        {
            return OfflineValidationResult.Fail(
                $"An unexpected error occurred during validation: {ex.Message}");
        }
    }

    // ── Base64Url Decoder ────────────────────────────────────────────
    // Keygen uses RFC 4648 URL-safe Base64 (- instead of +, _ instead of /)
    private static byte[] Base64UrlDecode(string base64Url)
    {
        if (string.IsNullOrWhiteSpace(base64Url))
            throw new FormatException("Base64Url string is empty.");

        string base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}