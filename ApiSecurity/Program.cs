using ApiSecurity.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Authentication service to allow the user to send back the token
builder.Services.AddAuthentication("Bearer")
   .AddJwtBearer(opts =>
   {
      opts.TokenValidationParameters = new()
      {

         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = builder.Configuration.GetValue<string>("Authentication:Issuer"),
         ValidAudience = builder.Configuration.GetValue<string>("Authentication:Audience"),
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
            builder.Configuration.GetValue<string>("Authentication:SecretKey")))
      };
   });

// Add fall back policy to make sure all controllers and methods are secure by default
builder.Services.AddAuthorization(opts =>
{
   // Add policy to confirm that a cliam exist(adding claims to the token doesn't necessary being passed)  
   opts.AddPolicy(PolicyConstants.MustHaveEmployeeId, policy =>
   {
      // policy.RequireAuthenticatedUser(); we can add this but it will be redundant
      // because we add it FallbackPolicy.
      policy.RequireClaim("employeeId");
   });

   opts.AddPolicy(PolicyConstants.MustBeTheOwner, policy =>
   {
      policy.RequireClaim("title", "Business Owner");
   });

   opts.AddPolicy(PolicyConstants.MustBeAVeteranEmployee, policy =>
   {
      policy.RequireClaim("employeeId", "E001", "E002", "E003");
   });


   opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                             .RequireAuthenticatedUser()
                             .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
