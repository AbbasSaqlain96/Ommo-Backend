using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Helpers.Constants
{
    public static class ErrorMessages
    {
        // Authentication & Authorization
        public const string InvalidCredentials = "Invalid username or password. Please check your credentials and try again.";

        // General Errors
        public const string InternalServerError = "An internal server error occurred. Please try again later.";
        public const string ServerDown = "Server is temporarily unavailable. Please try again later.";
        public const string OperationFailed = "The operation could not be completed. Please try again later.";
        public const string GenericOperationFailed = "An error occurred while processing the request. Please try again later.";

        // Database Errors
        public const string DatabaseOperationFailed = "An error occurred while accessing the database.";

        // Resource-Specific Errors with Placeholder Method
        public const string ResourceNotFoundTemplate = "{0} not found.";
        public static string ResourceNotFound(string resourceName) => string.Format(ResourceNotFoundTemplate, resourceName);

        // General error with specific context
        public const string ValidationFailed = "The provided input did not pass validation. Please check and try again.";

        public const string InvalidRequest = "Invalid request format.";
    }
}