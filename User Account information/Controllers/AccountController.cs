using ClassLibrary.Model.Models;
using ClassLibrary.Model.Models.DbModel;
using ClassLibrary.Model.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using User_Account_information.Data;

namespace User_Account_information.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] Login loginModel)
        {
            var user = await _userManager.FindByNameAsync(loginModel.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaim = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaim.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                        issuer: _configuration["JWT:ValidIssuer"],
                        audience: _configuration["JWT:ValidAudience"],
                        expires: DateTime.Now.AddHours(5),
                        claims: authClaim,
                        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                ; return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }

            return Unauthorized();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAdmin([FromBody] Register registerModel)
        {
            var userExists = await _userManager.FindByNameAsync(registerModel.Username);

            if (userExists != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User already Exists" });
            }

            IdentityUser user = new IdentityUser
            {
                Email = registerModel.Email,
                UserName = registerModel.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            //create user
            var result = await _userManager.CreateAsync(user, registerModel.Password);

            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User Creation failed! Please check the information and try again" });
            }

            //Checking if Roles Exist in database. If not exist, create new roles. 
            if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.Admin));

            if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.User))
                await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.User));

            //Add role to user
            if (!string.IsNullOrEmpty(registerModel.Role) && registerModel.Role == ApplicationUserRoles.Admin)
                await _userManager.AddToRoleAsync(user, ApplicationUserRoles.Admin);

            if (!string.IsNullOrEmpty(registerModel.Role) && registerModel.Role == ApplicationUserRoles.User)
                await _userManager.AddToRoleAsync(user, ApplicationUserRoles.User);

            var profile = new Profile
            {
                Address1 = registerModel.Address1,
                Address2 = registerModel.Address2,
                City = registerModel.City,
                State = registerModel.State,
                CountryCode = registerModel.CountryCode,
                Landmark = registerModel.Landmark,
                Pin = registerModel.Pin,
                UserId = user.Id
            };

            await _context.Profiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            return Ok(new Response { Status = "Success", Message = "User account was successfully created!" });
        }

        /// <summary>
        /// Generate Access Token
        /// </summary>
        /// <returns></returns>
        public async Task<RefreshTokenViewModel> GenerateAccessToken(IdentityUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id)
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddSeconds(300),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            var refreshToken = new RefreshTokenViewModel
            {
                RefreshToken = (await GenerateRefreshToken(user.Id, token.Id)).Token,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };

            return refreshToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="tokenId"></param>
        /// <returns><see cref="Task{RefreshToken}"/></returns>

        private async Task<RefreshToken> GenerateRefreshToken(string userId, string tokenId)
        {
            var refreshToken = new RefreshToken();
            var randomNumber = new byte[32];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
                refreshToken.CreatedDateUTC = DateTime.UtcNow;
                refreshToken.ExpirationDateUTC = DateTime.UtcNow.AddMonths(6);
                refreshToken.UserId = userId;
                refreshToken.JwtId = tokenId;
            }

            await _context.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("refreshtoken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenViewModel refreshToken)
        {
            var user = GetUserFromAccessToken(refreshToken.Token);

            if (user != null && ValidateRefreshToken(user, refreshToken.RefreshToken))
            {
                RefreshTokenViewModel refreshTokenViewModel = await GenerateAccessToken(user);

                return Ok(new
                {
                    success = true,
                    token = refreshTokenViewModel.Token,
                    refreshToken = refreshTokenViewModel.RefreshToken,
                    expiration = refreshTokenViewModel.Expiration
                });
            }
            return Unauthorized();
        }

        private object GetUserFromAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                IssuerSigningKey = authSigningKey,
                RequireExpirationTime = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken securityToken;

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        }
    }
}
