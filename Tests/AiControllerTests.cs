using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ParrotsAPI2.Controllers;
using ParrotsAPI2.Dtos.AiDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Ai;
using ParrotsAPI2.Services.User;

namespace parrotsAPI2.Tests;

public class AiControllerTests
{
    private static AiController CreateController(
        string? geminiResult,
        bool crackerDeductSuccess = true,
        string crackerMessage = "OK",
        int remainingBalance = 5,
        string userId = "user-1")
    {
        var aiService = new Mock<IAiService>();
        aiService.Setup(s => s.AskAsync(It.IsAny<AskParrotsQueryDto>(), It.IsAny<string>()))
                 .ReturnsAsync(geminiResult);

        var userService = new Mock<IUserService>();
        userService.Setup(s => s.DeductCrackerForAsk(It.IsAny<string>()))
                   .ReturnsAsync(new ServiceResponse<int>
                   {
                       Success = crackerDeductSuccess,
                       Message = crackerMessage,
                       Data = remainingBalance
                   });

        var controller = new AiController(aiService.Object, userService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"))
            }
        };
        return controller;
    }

    private static AskParrotsQueryDto MakeDto() => new()
    {
        VehicleType = "Car",
        Duration = "1 Day",
        Vibe = "Food",
        SpotType = "Hidden Gems",
        Latitude = 41.0,
        Longitude = 29.0
    };

    [Fact]
    public async Task Returns_200_with_response_and_balance_on_success()
    {
        var controller = CreateController("[[Istanbul, Kadıköy]] narrative", remainingBalance: 4);
        var result = await controller.Ask(MakeDto()) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("response", json);
        Assert.Contains("remainingBalance", json);
        Assert.Contains("4", json);
    }

    [Fact]
    public async Task Returns_500_when_gemini_returns_null()
    {
        var controller = CreateController(geminiResult: null);
        var result = await controller.Ask(MakeDto()) as ObjectResult;

        Assert.Equal(500, result!.StatusCode);
    }

    [Fact]
    public async Task Returns_500_when_gemini_response_does_not_start_with_brackets()
    {
        var controller = CreateController("Sure, here is a voyage...");
        var result = await controller.Ask(MakeDto()) as ObjectResult;

        Assert.Equal(500, result!.StatusCode);
    }

    [Fact]
    public async Task Returns_402_when_cracker_deduction_fails()
    {
        var controller = CreateController(
            "[[City, District]] narrative",
            crackerDeductSuccess: false,
            crackerMessage: "Insufficient crackers");

        var result = await controller.Ask(MakeDto()) as ObjectResult;

        Assert.Equal(402, result!.StatusCode);
    }

    [Fact]
    public async Task Returns_401_when_no_user_claim()
    {
        var aiService = new Mock<IAiService>();
        var userService = new Mock<IUserService>();
        var controller = new AiController(aiService.Object, userService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await controller.Ask(MakeDto());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Does_not_deduct_cracker_when_gemini_fails()
    {
        var aiService = new Mock<IAiService>();
        aiService.Setup(s => s.AskAsync(It.IsAny<AskParrotsQueryDto>(), It.IsAny<string>())).ReturnsAsync((string?)null);

        var userService = new Mock<IUserService>();

        var controller = new AiController(aiService.Object, userService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "mock"))
            }
        };

        await controller.Ask(MakeDto());

        userService.Verify(s => s.DeductCrackerForAsk(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Rate_limit_blocks_after_20_requests()
    {
        // Use a unique userId so rate limit state doesn't bleed across tests
        var userId = Guid.NewGuid().ToString();
        AiController controller = null!;

        IActionResult? lastResult = null;
        for (int i = 0; i <= 20; i++)
        {
            controller = CreateController("[[City, District]] narrative", userId: userId);
            lastResult = await controller.Ask(MakeDto());
        }

        var statusResult = lastResult as ObjectResult;
        Assert.Equal(429, statusResult!.StatusCode);
    }
}
