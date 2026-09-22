using finance.online.mvc.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;


namespace finance.online.mvc.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

        }

        [HttpGet("/auth")]
        public IActionResult Auth()
        {
            return View("~/Views/Auth/Index.cshtml");
        }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> ProxyLogin([FromBody] AuthLoginRequestDto model)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("https://localhost:7242/auth/login", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (result != null)
                {
                    Response.Cookies.Append("accessToken", result.AccessToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                    {
                        foreach (var cookie in cookieHeaders)
                        {
                            Response.Headers.Append("Set-Cookie", cookie);
                        }
                    }

                    return Ok();
                }
            }

            return await ForwardApiErrorAsync(response);
        }

        [HttpPost("/auth/register")]
        public async Task<IActionResult> ProxyRegister([FromBody] AuthRegisterRequestDto model)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("https://localhost:7242/auth/register", model);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }

            return await ForwardApiErrorAsync(response);
        }

        [HttpPost("/auth/logout")]
        [ValidateAntiForgeryToken]

        public IActionResult Logout()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return RedirectToAction("Index", "Home");
        }
    }
}