using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Controllers
{
	[ApiController]
	[Route("api/v1/auth")]
	public class AuthenticationController : ControllerBase
	{
		private readonly string _signingKey;

		public AuthenticationController(IConfiguration config)
		{
			// Ensure Jwt:Key is not null or empty
			var jwtKey = config.GetValue<string>("Jwt:Key");
			if (!string.IsNullOrWhiteSpace(jwtKey))
			{
				_signingKey = jwtKey;
			}
			else
			{
				throw new InvalidOperationException("Please set Jwt:Key in appsettings");
			}
		}

		[HttpPost("generatetoken")]
		public ActionResult<string> GenerateToken()
		{
			var keyBytes = Encoding.UTF8.GetBytes(_signingKey);
			var creds = new SigningCredentials(
								new SymmetricSecurityKey(keyBytes),
								SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
								claims: new[] { new Claim(ClaimTypes.Name, "dev-user") },
								expires: DateTime.UtcNow.AddHours(1),
								signingCredentials: creds);

			var jwt = new JwtSecurityTokenHandler().WriteToken(token);
			return Ok(jwt);
		}

		[Authorize]
		[HttpGet("validatetoken")]
		public IActionResult ValidateToken()
		{
			return Ok(new { valid = true });
		}
	}
}
