using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MessageBoardDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("MessageBoard")));

builder.Services.AddAntiforgery(options =>
{
    options.FormFieldName = "_token";
    options.HeaderName = "X-XSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles();

app.UseHttpMethodOverride(new HttpMethodOverrideOptions { FormFieldName = "_method" });

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/csrf-token", (IAntiforgery forgeryService, HttpContext context) =>
{
    var tokens = forgeryService.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken,
        new CookieOptions { HttpOnly = false });

    return Results.Ok();
});

app.Run();

public partial class Program { }
