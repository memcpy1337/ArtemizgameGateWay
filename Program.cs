using GateWay.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var identityServerSettings = new IdentityServerSettings();
builder.Configuration.GetSection("IdentityServerSettings").Bind(identityServerSettings);
builder.Services.AddSingleton(Options.Create(identityServerSettings));


#if !DEBUG
     identityServerSettings.AccessTokenSecret = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_SECRET");
#endif

builder.Services.AddAuthentication().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
{
    x.SaveToken = true;
    x.RequireHttpsMetadata = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identityServerSettings.AccessTokenSecret)),
        ValidateAudience = false,
        ValidateIssuer = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("author", p =>
    {
        p.RequireAuthenticatedUser();
    });
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run(); 