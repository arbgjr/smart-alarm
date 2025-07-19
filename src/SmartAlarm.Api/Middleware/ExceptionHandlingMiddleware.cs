using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAlarm.Api.Models;
using SmartAlarm.Api.Services;
using SmartAlarm.Domain.Exceptions;

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
                    var serviceProvider = context.RequestServices;
                    var errorMessageService = serviceProvider.GetService<IErrorMessageService>();
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("ExceptionHandlingMiddleware");

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    var errorResponse = CreateErrorResponse(exception, context, errorMessageService, logger);

                    context.Response.StatusCode = errorResponse.StatusCode;
                    context.Response.ContentType = "application/json";

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
                });
            });
        }

        private static ErrorResponse CreateErrorResponse(
            Exception? exception,
            HttpContext context,
            IErrorMessageService? errorMessageService,
            Microsoft.Extensions.Logging.ILogger? logger)
        {
            var traceId = context.TraceIdentifier;
            var timestamp = DateTime.UtcNow;

            // Log the exception
            logger?.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

            return exception switch
            {
                FluentValidation.ValidationException validationEx => CreateValidationErrorResponse(validationEx, traceId, timestamp, errorMessageService),
                NotFoundException notFoundEx => CreateNotFoundErrorResponse(notFoundEx, traceId, timestamp, errorMessageService),
                UnauthorizedAccessException unauthorizedEx => CreateUnauthorizedErrorResponse(unauthorizedEx, traceId, timestamp, errorMessageService),
                ArgumentException argumentEx => CreateBadRequestErrorResponse(argumentEx, traceId, timestamp, errorMessageService),
                _ => CreateInternalServerErrorResponse(exception, traceId, timestamp, errorMessageService)
            };
        }

        private static ErrorResponse CreateValidationErrorResponse(
            FluentValidation.ValidationException validationException,
            string traceId,
            DateTime timestamp,
            IErrorMessageService? errorMessageService)
        {
            var validationErrors = validationException.Errors.Select(error => new ValidationError
            {
                Field = error.PropertyName,
                Message = error.ErrorMessage,
                Code = error.ErrorCode,
                AttemptedValue = error.AttemptedValue
            }).ToList();

            return new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = errorMessageService?.GetMessage("System.ValidationFailed") ?? "Validation Failed",
                Detail = errorMessageService?.GetMessage("System.ValidationFailed") ?? "One or more validation errors occurred.",                
                Type = "ValidationError",
                TraceId = traceId,
                Timestamp = timestamp,
                ValidationErrors = validationErrors
            };
        }

        private static ErrorResponse CreateNotFoundErrorResponse(
            NotFoundException notFoundException,
            string traceId,
            DateTime timestamp,
            IErrorMessageService? errorMessageService)
        {
            return new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = notFoundException.Message,
                Type = "NotFoundError",
                TraceId = traceId,
                Timestamp = timestamp
            };
        }

        private static ErrorResponse CreateUnauthorizedErrorResponse(
            UnauthorizedAccessException unauthorizedException,
            string traceId,
            DateTime timestamp,
            IErrorMessageService? errorMessageService)
        {
            return new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Title = errorMessageService?.GetMessage("Authentication.AuthenticationRequired") ?? "Authentication Required",
                Detail = unauthorizedException.Message,
                Type = "AuthenticationError",
                TraceId = traceId,
                Timestamp = timestamp
            };
        }

        private static ErrorResponse CreateBadRequestErrorResponse(
            ArgumentException argumentException,
            string traceId,
            DateTime timestamp,
            IErrorMessageService? errorMessageService)
        {
            return new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = argumentException.Message,
                Type = "ValidationError",
                TraceId = traceId,
                Timestamp = timestamp
            };
        }

        private static ErrorResponse CreateInternalServerErrorResponse(
            Exception? exception,
            string traceId,
            DateTime timestamp,
            IErrorMessageService? errorMessageService)
        {
            return new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Title = errorMessageService?.GetMessage("System.InternalServerError") ?? "Internal Server Error",
                Detail = errorMessageService?.GetMessage("System.InternalServerError") ?? "An unexpected error occurred.",
                Type = "SystemError",
                TraceId = traceId,
                Timestamp = timestamp,
                Extensions = new Dictionary<string, object>
                {
                    ["exceptionType"] = exception?.GetType().Name ?? "Unknown"
                }
            };
        }
    }
}
