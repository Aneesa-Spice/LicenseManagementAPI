using System.Diagnostics;
using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Call the next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });

            // Return a standard error response
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (!context.Response.HasStarted)
        {
            // Set JSON response
            context.Response.ContentType = "application/json";

            // Default to Internal Server Error
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var result = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });

            await context.Response.WriteAsync(result);
        }
        else
        {
            // Response has started, cannot modify headers
            // Log the exception
            Console.WriteLine("Exception occurred after response started: " + ex);
        }
        //// Create a standard error object
        //var errorResponse = new
        //{
        //    message = "An unexpected error occurred. Please contact support.",
        //    // You can add more fields like: timestamp = DateTime.UtcNow
        //};
        //var result = JsonSerializer.Serialize(errorResponse);
        //return context.Response.WriteAsync(result);
    }
}