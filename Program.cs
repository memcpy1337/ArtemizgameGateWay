using Certes;
using FluffySpoon.AspNet.EncryptWeMust;
using FluffySpoon.AspNet.EncryptWeMust.Certes;
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

builder.Services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions()
{
    Email = "artemizgame@gmail.com", //LetsEncrypt will send you an e-mail here when the certificate is about to expire
    UseStaging = false, //switch to true for testing
    Domains = new[] { "api.artemizgame.ru" },
    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30), //renew automatically 30 days before expiry
    TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(7), //renew automatically 7 days after the last certificate was issued
    CertificateSigningRequest = new CsrInfo() //these are your certificate details
    {
        CountryName = "Russia",
        Locality = "RU",
        Organization = "Artemizgame",
        OrganizationUnit = "Hat department",
        State = "RU"
    }
});

builder.Services.AddFluffySpoonLetsEncryptFileCertificatePersistence();

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

app.UseFluffySpoonLetsEncrypt();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run(); 