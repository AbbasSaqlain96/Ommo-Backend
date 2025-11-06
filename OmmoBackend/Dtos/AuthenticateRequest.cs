namespace OmmoBackend.Dtos
{
    public class AuthenticateRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class AuthenticateSuccessResponse
    {
        public string Status { get; set; } = "valid";
        public string UserId { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public IDictionary<string, string>? Claims { get; set; }
    }

    public class AuthenticateErrorResponse
    {
        public string Status { get; set; } = "invalid";
        public string Message { get; set; } = string.Empty;
    }

}
