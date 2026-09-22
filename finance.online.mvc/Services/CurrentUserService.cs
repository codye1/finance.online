using finance.online.mvc.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace finance.online.mvc.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HomeUserModel? GetUser()
        {
            var accessToken = _httpContextAccessor.HttpContext?.Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(accessToken)) return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(accessToken)) return null;

                var jwtToken = handler.ReadJwtToken(accessToken);
                if (jwtToken.ValidTo <= DateTime.UtcNow)
                    return null;

                var userIdClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return null;

                return new HomeUserModel { Id = userIdClaim };
            }
            catch
            {
                return null;
            }
        }
    }
}
