using finance.online.api.Models;
using finance.online.api.Models.DTO;
using finance.online.api.Repositories.RefreshTokenRepository;
using finance.online.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace finance.online.api.Controllers
{
    [Route("/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _tokenRepository;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService,
            IRefreshTokenRepository tokenRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _tokenRepository = tokenRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(ApiErrors.FromIdentityErrors(result.Errors));
            }

            return Ok("User registered!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return Unauthorized(ApiErrors.General("Invalid email or password."));
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return Unauthorized(ApiErrors.General("Invalid email or password."));
            }

            var response = await IssueTokensAsync(user);
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokenValue = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshTokenValue))
            {
                return Unauthorized(ApiErrors.General("No refresh token provided."));
            }

            var existingToken = await _tokenRepository.GetByTokenAsync(refreshTokenValue);

            if (existingToken == null || !existingToken.IsActive)
            {
                return Unauthorized(ApiErrors.General("Invalid or expired refresh token."));
            }

            existingToken.RevokedAt = DateTime.UtcNow;
            _tokenRepository.Update(existingToken);

            var response = await IssueTokensAsync(existingToken.User, existingToken);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshTokenValue = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshTokenValue))
            {
                var existingToken = await _tokenRepository.GetByTokenAsync(refreshTokenValue);

                if (existingToken != null && existingToken.IsActive)
                {
                    existingToken.RevokedAt = DateTime.UtcNow;
                    _tokenRepository.Update(existingToken);
                    await _tokenRepository.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("refreshToken");
            return Ok("Logged out");
        }

        [HttpGet("check")]
        [Authorize]
        public IActionResult Check()
        {
            return Ok("Ok");
        }

        private async Task<AuthResponseDto> IssueTokensAsync(AppUser user, RefreshToken? oldToken = null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            if (oldToken != null)
            {
                oldToken.ReplacedByToken = refreshTokenValue;
                _tokenRepository.Update(oldToken);
            }

            _tokenRepository.Add(refreshToken);
            await _tokenRepository.SaveChangesAsync();

            Response.Cookies.Append("refreshToken", refreshTokenValue, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresAt
            });

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = _tokenService.AccessTokenExpiry
            };
        }
    }
}