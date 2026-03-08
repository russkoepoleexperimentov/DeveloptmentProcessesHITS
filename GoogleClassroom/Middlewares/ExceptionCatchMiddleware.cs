using Common;
using Common.Exceptions;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Http;
using System;

namespace Common.Middlewares
{
    public class ExceptionCatchMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionCatchMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (StatusCodeException ex)
            {
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception: {ex.Message}");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception is StatusCodeException statusCodeException ?
                statusCodeException.StatusCode :
                StatusCodes.Status500InternalServerError;

            var response = new ApiResponse<object>
            {
                Type = ApiResponseType.Error,
                Message = exception.Message,
                Data = null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}