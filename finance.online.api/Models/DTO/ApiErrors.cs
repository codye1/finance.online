using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace finance.online.api.Models.DTO
{
    public static class ApiErrors
    {
        public const string GeneralKey = "_general";

        public static ApiErrorResponseDto Single(string code, string message)
        {
            return FromPairs((code, message));
        }

        public static ApiErrorResponseDto General(string message)
        {
            return FromPairs((GeneralKey, message));
        }

        public static ApiErrorResponseDto FromDictionary(IDictionary<string, IEnumerable<string>> errors)
        {
            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    AddError(groupedErrors, error.Key, message);
                }
            }

            return Build(groupedErrors);
        }

        public static ApiErrorResponseDto FromIdentityErrors(IEnumerable<IdentityError> errors)
        {
            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var error in errors)
            {
                AddError(groupedErrors, MapIdentityErrorKey(error.Code), error.Description);
            }

            return Build(groupedErrors);
        }

        public static ApiErrorResponseDto FromModelState(ModelStateDictionary modelState)
        {
            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in modelState)
            {
                var key = string.IsNullOrWhiteSpace(entry.Key) ? GeneralKey : entry.Key;

                foreach (var error in entry.Value?.Errors ?? Enumerable.Empty<ModelError>())
                {
                    var message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The submitted value is invalid."
                        : error.ErrorMessage;

                    AddError(groupedErrors, key, message);
                }
            }

            if (groupedErrors.Count == 0)
            {
                AddError(groupedErrors, GeneralKey, "The submitted data is invalid.");
            }

            return Build(groupedErrors);
        }

        public static ApiErrorResponseDto FromStatusCode(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status401Unauthorized => General("Authentication is required."),
                StatusCodes.Status403Forbidden => General("You do not have permission to access this resource."),
                StatusCodes.Status404NotFound => General("Resource not found."),
                StatusCodes.Status405MethodNotAllowed => General("The requested method is not allowed."),
                _ => General("An unexpected error occurred.")
            };
        }

        public static ApiErrorResponseDto FromPairs(params (string Key, string Message)[] errors)
        {
            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var error in errors)
            {
                AddError(groupedErrors, error.Key, error.Message);
            }

            return Build(groupedErrors);
        }

        private static ApiErrorResponseDto Build(Dictionary<string, List<string>> errors)
        {
            return new ApiErrorResponseDto
            {
                Message = "Request failed",
                Errors = errors.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.OrdinalIgnoreCase)
            };
        }

        private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = new List<string>();
                errors[key] = messages;
            }

            messages.Add(message);
        }

        private static string MapIdentityErrorKey(string code)
        {
            return code switch
            {
                "DuplicateUserName" => "email",
                "DuplicateEmail" => "email",
                "InvalidEmail" => "email",
                "PasswordTooShort" => "password",
                "PasswordRequiresDigit" => "password",
                "PasswordRequiresLower" => "password",
                "PasswordRequiresUpper" => "password",
                "PasswordRequiresNonAlphanumeric" => "password",
                "PasswordRequiresUniqueChars" => "password",
                _ => GeneralKey
            };
        }
    }
}
