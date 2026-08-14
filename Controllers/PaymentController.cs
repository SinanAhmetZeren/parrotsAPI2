using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace parrotsAPI2.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private static Dictionary<string, (int Crackers, decimal Eur)> BuildPriceMap()
    {
        var map = new Dictionary<string, (int, decimal)>();
        void Add(string? key, int crackers, decimal eur) { if (!string.IsNullOrEmpty(key)) map[key] = (crackers, eur); }
        Add(Environment.GetEnvironmentVariable("Paddle__PriceSandboxNest"),  100, 2.99m);
        Add(Environment.GetEnvironmentVariable("Paddle__PriceLiveNest"),     100, 2.99m);
        Add(Environment.GetEnvironmentVariable("Paddle__PriceLiveFlock"),    250, 5.99m);
        Add(Environment.GetEnvironmentVariable("Paddle__PriceLiveColony"),   500, 9.99m);
        return map;
    }

    private static readonly Dictionary<string, (int Crackers, decimal Eur)> _priceMap = BuildPriceMap();

    private readonly DataContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(DataContext context, IConfiguration config, ILogger<PaymentController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var body = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var secret = Environment.GetEnvironmentVariable("Paddle__WebhookSecret") ?? "";

        if (!Request.Headers.TryGetValue("Paddle-Signature", out var signatureHeader) ||
            !VerifyPaddleSignature(signatureHeader.ToString(), body, secret))
        {
            _logger.LogWarning("Paddle webhook signature verification failed.");
            return Unauthorized();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Paddle webhook JSON body.");
            return BadRequest();
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("event_type", out var eventTypeProp) ||
                eventTypeProp.GetString() != "transaction.completed")
                return Ok();

            var data = root.GetProperty("data");

            var transactionId = data.TryGetProperty("id", out var tid) ? tid.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(transactionId)) return Ok();

            var alreadyProcessed = await _context.CrackerPurchases.AnyAsync(p => p.PaymentProviderId == transactionId);
            if (alreadyProcessed) return Ok();

            if (!data.TryGetProperty("custom_data", out var customData) ||
                !customData.TryGetProperty("userId", out var userIdProp))
            {
                _logger.LogWarning("Paddle webhook transaction {TransactionId} missing custom_data.userId", transactionId);
                return Ok();
            }

            var userId = userIdProp.GetString() ?? "";
            if (string.IsNullOrEmpty(userId)) return Ok();

            if (!data.TryGetProperty("items", out var items) || items.GetArrayLength() == 0) return Ok();

            int totalCrackers = 0;
            decimal totalEur = 0;

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("price", out var price)) continue;
                if (!price.TryGetProperty("id", out var priceIdProp)) continue;
                var priceId = priceIdProp.GetString() ?? "";
                var quantity = item.TryGetProperty("quantity", out var qProp) && qProp.TryGetInt32(out var q) ? q : 1;

                if (_priceMap.TryGetValue(priceId, out var tier))
                {
                    totalCrackers += tier.Crackers * quantity;
                    totalEur += tier.Eur * quantity;
                }
            }

            if (totalCrackers == 0)
            {
                _logger.LogWarning("No recognized price IDs found in transaction {TransactionId}", transactionId);
                return Ok();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError("User {UserId} not found for transaction {TransactionId}", userId, transactionId);
                return Ok();
            }

            user.ParrotCrackerBalance += totalCrackers;

            _context.CrackerPurchases.Add(new CrackerPurchase
            {
                UserId = userId,
                CrackersAmount = totalCrackers,
                EurAmount = totalEur,
                Status = "completed",
                PaymentProviderId = transactionId,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Credited {Crackers} crackers to user {UserId} via transaction {TransactionId}", totalCrackers, userId, transactionId);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("duplicate key") == true ||
                ex.InnerException?.Message.Contains("unique constraint") == true)
            {
                _logger.LogInformation("Duplicate transaction {TransactionId} caught during DB save.", transactionId);
                return Ok();
            }
        }

        return Ok();
    }

    private static bool VerifyPaddleSignature(string header, string body, string secret)
    {
        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(secret)) return false;

        string? ts = null;
        string? h1 = null;
        foreach (var part in header.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0] == "ts") ts = kv[1];
            else if (kv[0] == "h1") h1 = kv[1];
        }

        if (ts == null || h1 == null) return false;

        if (!long.TryParse(ts, out var unixTs)) return false;
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unixTs) > 300) return false;

        var payload = $"{ts}:{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        byte[] expected;
        try { expected = Convert.FromHexString(h1); }
        catch { return false; }

        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }
}
