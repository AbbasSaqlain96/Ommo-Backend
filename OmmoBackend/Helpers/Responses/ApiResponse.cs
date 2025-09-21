using Microsoft.AspNetCore.Mvc;

namespace OmmoBackend.Helpers.Responses
{
    public static class ApiResponse
    {
        public static IActionResult Success(object data, string message = "Operation successful.")
        {
            return new OkObjectResult(new { message, data });
        }

        public static IActionResult Error(string errorMessage, int statusCode)
        {
            return new ObjectResult(new { errorMessage }) { StatusCode = statusCode };
        }
    }
}