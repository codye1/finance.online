using System.Text.Json.Serialization;

namespace finance.online.api.Models.DTO
{
    public class ApiErrorResponseDto
    {
        public string Message { get; set; } = "Request failed";

        public Dictionary<string, string[]> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonConstructor]
        public ApiErrorResponseDto()
        {
        }

        public ApiErrorResponseDto(string message, Dictionary<string, string[]> errors)
        {
            Message = message;
            Errors = errors;
        }
    }
}