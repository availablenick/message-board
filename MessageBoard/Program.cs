using Microsoft.EntityFrameworkCore;

using MessageBoard.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MessageBoardDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("MessageBoard")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
