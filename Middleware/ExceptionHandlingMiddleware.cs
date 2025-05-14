// Middleware/ExceptionHandlingMiddleware.cs
using System.Text.Json;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            Console.WriteLine("---------------------------------");
            await _next(context); // Proceed to next middleware
            
            // If a 404 error occurred and no response has been started yet
            if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
            {
                context.Response.ContentType = "text/html";
                context.Response.StatusCode = 404;

                // Return a custom 404 HTML page
                await context.Response.WriteAsync(Get404HtmlPage());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";

            var response = _env.IsDevelopment()
                ? $"<h1>Internal Server Error</h1><p>{ex.Message}</p><pre>{ex.StackTrace}</pre>"
                : "<h1>Internal Server Error</h1><p>Something went wrong.</p>";

            await context.Response.WriteAsync(response);
        }
    }

    private string Get404HtmlPage()
    {
        // Simple HTML page for 404 error
        return @"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>404 - Not Found</title>
                <style>
                    body { font-family: Arial, sans-serif; padding: 20px; }
                    h1 { color: #e74c3c; }
                    p { font-size: 18px; }
                    a { color: #3498db; text-decoration: none; }
                </style>
            </head>
            <body>
                <h1>404 - Not Found</h1>
                <p>Sorry.... the page you're looking for does not exist.</p>
                <p><a href='/'>Go back to the homepage</a></p>
            </body>
            </html>
        ";
    }
}
