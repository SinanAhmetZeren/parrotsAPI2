using ParrotsAPI2.Helpers;

namespace parrotsAPI2.Tests;

public class EncryptionHelperTests
{
    private static byte[] MakeKey() => new byte[32]; // 32-byte zero key — valid AES-256

    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalText()
    {
        var key = MakeKey();
        var original = "Hello, Parrots!";

        var encrypted = EncryptionHelper.EncryptString(original, key);
        var decrypted = EncryptionHelper.DecryptString(encrypted, key);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        var key = MakeKey();
        var text = "same plaintext";

        var cipher1 = EncryptionHelper.EncryptString(text, key);
        var cipher2 = EncryptionHelper.EncryptString(text, key);

        // IV is randomized per call, so ciphertexts should differ
        Assert.NotEqual(cipher1, cipher2);
    }

    [Fact]
    public void Encrypt_ProducesBase64Output()
    {
        var key = MakeKey();
        var encrypted = EncryptionHelper.EncryptString("test", key);

        var bytes = Convert.FromBase64String(encrypted); // throws if not valid base64
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void KeyFromBase64_RoundTrips()
    {
        var original = new byte[32];
        new Random(42).NextBytes(original);
        var base64 = Convert.ToBase64String(original);

        var result = EncryptionHelper.KeyFromBase64(base64);

        Assert.Equal(original, result);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsOrReturnsGarbage()
    {
        var key1 = new byte[32];
        var key2 = new byte[32];
        key2[0] = 0xFF; // different key

        var encrypted = EncryptionHelper.EncryptString("secret", key1);

        // Wrong key should throw a CryptographicException (bad padding)
        Assert.ThrowsAny<Exception>(() => EncryptionHelper.DecryptString(encrypted, key2));
    }

    [Fact]
    public void Encrypt_EmptyString_EncryptsAndDecryptsCorrectly()
    {
        var key = MakeKey();

        var encrypted = EncryptionHelper.EncryptString(string.Empty, key);
        var decrypted = EncryptionHelper.DecryptString(encrypted, key);

        Assert.Equal(string.Empty, decrypted);
    }
}
