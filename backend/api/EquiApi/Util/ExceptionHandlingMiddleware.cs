using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Util;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request to {Path}",
                            context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred"
            };
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?) null,
                                                    "application/problem+json");
        }
    }
}