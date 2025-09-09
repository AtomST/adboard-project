﻿using AdBoard.API.Models.Responses;
using AdBoard.Services.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using ValidationException = FluentValidation.ValidationException;

namespace AdBoard.API.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IOptions<JsonOptions> jsonOptons)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, jsonOptons.Value.JsonSerializerOptions);
            }
        }

        public Task HandleExceptionAsync(HttpContext context, Exception exception, JsonSerializerOptions jsonSerializerOptions)
        {
            var result = new ErrorResponse
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Message = "Unhandled server error",
            };

            switch (exception)
            {
                case ValidationException validationException:
                {
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.Message = "Ошибка валидации";
                    result.Errors = validationException.Errors?.Select(e => new
                    {
                        Property = e.PropertyName,
                        Error = e.ErrorMessage
                    });
                    break;
                };
                case NotImplementedException implementedException:
                {
                    result.StatusCode = HttpStatusCode.NotImplemented;
                    result.Message = "Функционал не реализован";
                    break;
                };
                case ForbiddenException forbiddenException:
                {
                    result.StatusCode = HttpStatusCode.Forbidden;
                    result.Message = forbiddenException.Message;
                    break;
                };
                case InvalidInputException notFoundException:
                {
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.Message = notFoundException.Message;
                    break;
                }
                case UnauthorizedException unauthorizedException:
                {
                    result.StatusCode = HttpStatusCode.Unauthorized;
                    result.Message = unauthorizedException.Message;
                    break;
                }
                case RecordExistsException recordExistsException:
                {
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.Message = recordExistsException.Message;
                    break;
                };
                case NotFoundException notFoundException:
                {
                    result.StatusCode = HttpStatusCode.NotFound;
                    result.Message = notFoundException.Message;
                    break;
                }
                ;
                case Exception ex:
                {
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    break;
                }
            }

            context.Response.StatusCode = (int)result.StatusCode;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(result, jsonSerializerOptions));
        }
    }
}
