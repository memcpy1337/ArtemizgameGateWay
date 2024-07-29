using GateWay.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
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
#if !DEBUG
app.UseHttpsRedirection();
#endif
app.MapReverseProxy(proxyPipeline =>
{
    // Use a custom proxy middleware, defined below
#if DEBUG
    proxyPipeline.Use(DebugStep);
#endif
    // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
    proxyPipeline.UseSessionAffinity();
    proxyPipeline.UseLoadBalancing();
});

Task DebugStep(HttpContext context, Func<Task> next)
{
    // Can read data from the request via the context
    foreach (var header in context.Request.Headers)
    {
        Console.WriteLine($"{header.Key}: {header.Value}");
    }

    Console.WriteLine(context.Request.Headers["X-Forwarded-For"].ToString());

    // The context also stores a ReverseProxyFeature which holds proxy specific data such as the cluster, route and destinations
    var proxyFeature = context.GetReverseProxyFeature();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(proxyFeature.Route.Config));

    // Important - required to move to the next step in the proxy pipeline
    return next();
}

app.Run(); 