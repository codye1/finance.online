using System.Net;
using System.Net.Http.Headers;
using finance.online.mvc.Models.DTO;

namespace finance.online.mvc.Handlers
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AuthHeaderHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory factory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = factory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            string? currentToken = context?.Request.Cookies["accessToken"];

            if (!string.IsNullOrEmpty(currentToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
            }

            MemoryStream? backupStream = null;
            if (request.Content != null)
            {
                backupStream = new MemoryStream();
                await request.Content.CopyToAsync(backupStream, cancellationToken);
                backupStream.Position = 0;

                request.Content = CreateStreamContent(backupStream, request.Content.Headers);
            }

            var response = await base.SendAsync(request, cancellationToken);
            var requestUrl = request.RequestUri?.AbsolutePath ?? "";

            if (response.StatusCode == HttpStatusCode.Unauthorized
                && !requestUrl.Contains("/auth/login")
                && !requestUrl.Contains("/auth/refresh"))
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    string? latestToken = _httpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

                    if (!string.IsNullOrEmpty(latestToken) && latestToken != currentToken)
                    {
                        var shortCircuitRequest = CloneRequest(request, backupStream);
                        shortCircuitRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", latestToken);
                        return await base.SendAsync(shortCircuitRequest, cancellationToken);
                    }

                    var (isRefreshed, newAccessToken) = await TryRefreshTokenAsync(context);

                    if (isRefreshed && !string.IsNullOrEmpty(newAccessToken))
                    {
                        var retriedRequest = CloneRequest(request, backupStream);
                        retriedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        return await base.SendAsync(retriedRequest, cancellationToken);
                    }
                    else
                    {
                        context?.Response.Cookies.Delete("accessToken");
                        context?.Response.Cookies.Delete("refreshToken");

                        return response;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return response;
        }

        private async Task<(bool Success, string? AccessToken)> TryRefreshTokenAsync(HttpContext? context)
        {
            if (context == null) return (false, null);

            try
            {
                var refreshToken = context.Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken)) return (false, null);

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:7094");

                var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
                refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");

                var response = await client.SendAsync(refreshRequest);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (data != null && !string.IsNullOrEmpty(data.AccessToken))
                    {
                        context.Response.Cookies.Append("accessToken", data.AccessToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        });

                        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                        {
                            foreach (var cookie in cookieHeaders)
                            {
                                context.Response.Headers.Append("Set-Cookie", cookie);
                            }
                        }

                        return (true, data.AccessToken);
                    }
                }
            }
            catch
            {
                return (false, null);
            }

            return (false, null);
        }

        private HttpRequestMessage CloneRequest(HttpRequestMessage req, MemoryStream? backupStream)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            foreach (var prop in req.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key.ToString()), prop.Value);
            }

            if (backupStream != null)
            {
                backupStream.Position = 0;
                clone.Content = CreateStreamContent(backupStream, req.Content!.Headers);
            }

            return clone;
        }

        private StreamContent CreateStreamContent(MemoryStream stream, HttpContentHeaders originalHeaders)
        {
            var content = new StreamContent(stream);
            foreach (var header in originalHeaders)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            return content;
        }
    }
}