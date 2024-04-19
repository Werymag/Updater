using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add web api controllers as services 
builder.Services.AddMvc().AddControllersAsServices();


// Authentication with cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/Home/login");
builder.Services.AddAuthorization();

// Clear all providers
builder.Logging.ClearProviders();
// Log to console
builder.Logging.AddConsole();   

var app = builder.Build();


app.Configuration["login"] = Environment.GetEnvironmentVariable("login");
app.Configuration["password"] = Environment.GetEnvironmentVariable("password");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();

    app.Configuration["login"] = "admin";
    app.Configuration["password"] = "admin";
}

app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации 

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
