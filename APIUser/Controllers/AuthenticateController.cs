using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Cors;

namespace APIUser.Controllers
{
    // [Route("api/[controller]")]
    [EnableCors("AllowOrigin")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
         private readonly UserManager<User> userManager;
        // private readonly RoleManager<IdentityRole> roleManager;
         private readonly IConfiguration _configuration;

         public AuthenticateController(UserManager<User> userManager,
            /* RoleManager<IdentityRole> roleManager,*/ IConfiguration configuration)
         {
             this.userManager = userManager;
             //this.roleManager = roleManager;
             _configuration = configuration;
         }

         [HttpPost]
         [Route("login")]
         public async Task<IActionResult> Login([FromBody] LoginModel model)
         {
             var user = await userManager.FindByNameAsync(model.Username);
             if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
             {
                 var userRoles = await userManager.GetRolesAsync(user);

                 var authClaims = new List<Claim>
                 {
                     new Claim(ClaimTypes.Name, user.UserName),
                     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                 };

                 foreach (var userRole in userRoles)
                 {
                     authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                 }

                 var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                 var token = new JwtSecurityToken(
                     issuer: _configuration["JWT:ValidIssuer"],
                     audience: _configuration["JWT:ValidAudience"],
                     expires: DateTime.Now.AddHours(3),
                     claims: authClaims,
                     signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                     );

                 return Ok(new
                 {
                     token = new JwtSecurityTokenHandler().WriteToken(token),
                     expiration = token.ValidTo
                 });
             }
             return Unauthorized();
         }

         [HttpPost]
         [Route("register")]
         public async Task<IActionResult> Register([FromBody] RegisterModel model)
         {
             var userExists = await userManager.FindByNameAsync(model.Username);
             if (userExists != null)
                 return StatusCode(StatusCodes.Status500InternalServerError, new ResultViewModel { IsSucess = false, Message = "User already exists!" });

             User user = new User()
             {
                 Email = model.Email,
                 SecurityStamp = Guid.NewGuid().ToString(),
                 UserName = model.Username
             };
             var result = await userManager.CreateAsync(user, model.Password);
             if (!result.Succeeded)
                 return StatusCode(StatusCodes.Status500InternalServerError, new ResultViewModel { IsSucess = false, Message = "User creation failed! Please check user details and try again." });

             return Ok(new ResultViewModel { IsSucess = true, Message = "User created successfully!" });
         }

        /* [HttpPost]
         [Route("register-admin")]
         public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
         {
             var userExists = await userManager.FindByNameAsync(model.Username);
             if (userExists != null)
                 return StatusCode(StatusCodes.Status500InternalServerError, new ResultViewModel { IsSucess = false, Message = "User already exists!" });

             User user = new User()
             {
                 Email = model.Email,
                 SecurityStamp = Guid.NewGuid().ToString(),
                 UserName = model.Username
             };
             var result = await userManager.CreateAsync(user, model.Password);
             if (!result.Succeeded)
                 return StatusCode(StatusCodes.Status500InternalServerError, new ResultViewModel { IsSucess = false, Message = "User creation failed! Please check user details and try again." });

             if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                 await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
             if (!await roleManager.RoleExistsAsync(UserRoles.User))
                 await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

             if (await roleManager.RoleExistsAsync(UserRoles.Admin))
             {
                 await userManager.AddToRoleAsync(user, UserRoles.Admin);
             }

             return Ok(new ResultViewModel { IsSucess = true, Message = "User created successfully!" });
         }*/
    }
}
