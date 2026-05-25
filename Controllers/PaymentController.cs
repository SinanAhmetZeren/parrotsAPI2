using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace parrotsAPI2.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private static readonly Dictionary<int, decimal> _coinTiers = new()
    {
        { 100,   3.00m },
        { 1000,  30.00m },
        { 10000, 300.00m },
    };

    private readonly DataContext _context;
    private readonly IConfiguration _config;

    public PaymentController(DataContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("create-intent")]
    [Authorize]
    public async Task<IActionResult> CreateIntent([FromBody] CreateIntentRequest req)
    {
        if (!_coinTiers.TryGetValue(req.Coins, out var eurAmount))
            return BadRequest("Invalid coin package.");

        var amountCents = (long)(eurAmount * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = "eur",
            Metadata = new Dictionary<string, string>
            {
                { "userId", req.UserId },
                { "coins", req.Coins.ToString() },
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        return Ok(new { clientSecret = intent.ClientSecret });
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        HttpContext.Request.EnableBuffering();
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        HttpContext.Request.Body.Position = 0;
        var webhookSecret = _config.GetSection("Stripe")["WebhookSecret"] ?? "";

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook signature verification failed: {ex.Message}");
            Console.WriteLine($"Stripe-Signature header: {Request.Headers["Stripe-Signature"]}");
            Console.WriteLine($"Body length: {json.Length}");
            Console.WriteLine($"Secret length: {webhookSecret.Length}");
            return BadRequest();
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
        {
            var intent = stripeEvent.Data.Object as PaymentIntent;
            if (intent == null) return Ok();

            var paymentIntentId = intent.Id;
            var alreadyProcessed = _context.CoinPurchases
                .Any(p => p.PaymentProviderId == paymentIntentId);

            if (alreadyProcessed) return Ok();

            if (!intent.Metadata.TryGetValue("userId", out var userId)) return Ok();
            if (!intent.Metadata.TryGetValue("coins", out var coinsStr)) return Ok();
            if (!int.TryParse(coinsStr, out var coins)) return Ok();
            if (!_coinTiers.TryGetValue(coins, out var eurAmount)) return Ok();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Ok();

            user.ParrotCoinBalance += coins;

            _context.CoinPurchases.Add(new CoinPurchase
            {
                UserId = userId,
                CoinsAmount = coins,
                EurAmount = eurAmount,
                Status = "completed",
                PaymentProviderId = paymentIntentId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}

public class CreateIntentRequest
{
    public string UserId { get; set; } = "";
    public int Coins { get; set; }
}
