using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiSecurity.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
   private readonly IConfiguration _config;
   public AuthenticationController(IConfiguration config)
   {
      _config = config;
   }
   // Create an object/model to hold the user name and password
   // A record is the equivelant of creating a class and have 2 properties (e.g. UserName and Password)
   // and set a constructor to get in these values. Its a read only object
   public record AuthenticationData(string? UserName, string? Password);
   public record UserData(int UserId, string UserName);
   // api/Authentication/token
   [HttpPost("token")]
   public ActionResult<string> Authenticate([FromBody] AuthenticationData data)
   {  // Passing basic auth (user name and password in the body) verified it against the db or something else
      var user = ValidateCredentials(data);
      if (user is null)
      {
         return Unauthorized();  // 401
      }
      // Generate a token in case of authorization success
      var token = GenerateToken(user);

      return Ok(token);
   }
   private string GenerateToken(UserData user)
   {
      // We generate a token, so the user doesn’t have to put his credential
      // every time he / she makes a call for our api for a period of time.

      // This step is to secure the token so its not easy to genearte
      var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
         _config.GetValue<string>("Authentication:SecretKey")));

      // Sign the token. It will take the token use the secret key to sign it(digital signature)
      // If anything change sinside the token will break everything because its not matching the signature
      // Verifiaction step
      var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

      // Add claims: Data points about the user that we are verifying
      // In our token, we can have standard claims or custom claims
      List<Claim> claims = new();
      // Standar claims (industry standars)
      claims.Add(new(JwtRegisteredClaimNames.Sub, user.UserId.ToString())); // The subject what identify the user
      claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));


      // Build the token. In authentications system we may have a regular token and a refresh token
      var token = new JwtSecurityToken(
            _config.GetValue<string>("Authentication:Issuer"),
            _config.GetValue<string>("Authentication:Audience"),
            claims,
            DateTime.UtcNow, // When this token beocmes valid (number od seconds since jan 1st, 1970)
            DateTime.UtcNow.AddMinutes(1), //This when the token will expire
            signingCredentials);

      return new JwtSecurityTokenHandler().WriteToken(token);
   }

   private UserData? ValidateCredentials(AuthenticationData data)
   {
      // In real life scenarios we will be using for example (Database user record, Azure active directory, Auth0, etc.)
      // But for this demo, we are focusing on creating token only. and the following code will be simulating
      // connecting to the db and verified if there is a record match the user
      if (CompareValues(data.UserName, "ishobaki") &&
         CompareValues(data.Password, "Test123"))
      {
         return new UserData(1, data.UserName!);  //! mark is t tell the compiler to ignore
                                                  // it becuase we are sure that it will not null
      }

      if (CompareValues(data.UserName, "sstorm") &&
        CompareValues(data.Password, "Test123"))
      {
         return new UserData(2, data.UserName!);
      }

      return null;
   }

   private bool CompareValues(string? actual, string expected)
   {
      if (actual is not null)
      {
         if (actual.Equals(expected))//, StringComparison.InvariantCultureIgnoreCase))
         {
            return true;
         }
      }
      return false;
   }
}
