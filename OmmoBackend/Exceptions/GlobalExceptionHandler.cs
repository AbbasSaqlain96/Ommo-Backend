using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OmmoBackend.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger; // Logger for capturing and recording exception details

        /// <summary>
        /// Initializes a new instance of the GlobalExceptionHandler class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger for capturing and recording exceptions.</param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles exceptions that occur during the request processing.
        /// </summary>
        /// <param name="httpContext">The HTTP context for the current request.</param>
        /// <param name="exception">The exception that occurred during request processing.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation, with a boolean indicating if the exception was handled.</returns>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Log the exception details
            _logger.LogError(exception, "Exception occured: {Message}", exception.Message);

            // Create a ProblemDetails object to describe the error
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError, // HTTP status code for server error
                Title = "Server Error" // Title for the problem details
            };

            // Set the response status code
            httpContext.Response.StatusCode = problemDetails.Status.Value;

            // Write the problem details to the response
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; // Indicate that the exception has been handled
        }
    }
}