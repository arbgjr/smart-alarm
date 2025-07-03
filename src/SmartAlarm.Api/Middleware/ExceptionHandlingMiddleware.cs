using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace SmartAlarm.Api.Middleware
{
    public static class ExceptionHandlingMiddleware
    {
        public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;
                    Log.Error(exception, "Unhandled exception");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var problem = new ProblemDetails
                    {
                        Status = context.Response.StatusCode,
                        Title = "An unexpected error occurred.",
                        Detail = exception?.Message
                    };
                    await context.Response.WriteAsJsonAsync(problem);
                });
            });
        }
    }
}
