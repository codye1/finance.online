namespace finance.online.mvc.Models.DTO
{
    public class ApiErrorResponseDto
    {
        public string Message { get; set; } = "Request failed";

        public Dictionary<string, string[]> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
