using ApiCisco;
using ApiEVE;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using DataLayer;
using DataLayer.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SimpleLogger;
using System.Globalization;
using System.Threading.RateLimiting;

namespace NetResVM.Web
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    public class Program
	{
        /// <summary>
        /// Main method to start the application.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
		{		
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
			builder.Services.AddControllersWithViews(options =>
			{
				options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
				options.Filters.Add(new AuthorizeFilter());
			})
			.AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
			.AddDataAnnotationsLocalization();
			builder.Services.AddSession(options =>
			{
                options.Cookie.Name = "SessionCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.IsEssential = true;
            });

            // Dependency Injection for services
            builder.Services.AddScoped<IServerService, ServerService>();
            builder.Services.AddScoped<ILocalBackupStorage, LocalBackupStorage>();
            builder.Services.AddScoped<BackupService>();
            builder.Services.AddScoped<ServerService>();
            builder.Services.AddScoped<ReservationService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<UserLabOwnershipService>();


            builder.Services.AddScoped<IPlatformManager, PlatformManager>();
            builder.Services.AddScoped<IVirtualizationAdapter, CiscoCmlAdapter>();
            builder.Services.AddScoped<IVirtualizationAdapter, EveNGAdapter>();

            builder.Services.AddSingleton<BackgroundTask>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(options =>
			{
				options.Cookie.Name = "AuthCookies";
				options.Cookie.HttpOnly = true;
				options.Cookie.SameSite = SameSiteMode.Lax;
				options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
				options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
				options.SlidingExpiration = true;
                options.LoginPath = "/Login/Index";
                options.LogoutPath = "/Login/Logout";
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var username = context.Principal?.Identity?.Name;
                        if (string.IsNullOrEmpty(username))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
                        if (!userService.IsUserActive(username))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    }
                };
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("login-policy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.HttpContext.Request.Headers.Accept.ToString().Contains("text/html"))
                    {
                        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                        await context.HttpContext.Response.WriteAsync(
                            "<!DOCTYPE html><html><head><meta charset='utf-8'><title>429 Too Many Requests</title>" +
                            "<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css'></head>" +
                            "<body class='container py-5 text-center'>" +
                            "<div class='alert alert-warning py-4'><h4>Příliš mnoho pokusů o přihlášení (Rate Limit)</h4>" +
                            "<p>Z bezpečnostních důvodů byl přístup dočasně pozastaven. Zkuste to prosím znovu za 1 minutu.</p>" +
                            "<a href='/Login/Index' class='btn btn-primary mt-3'>Zpět na přihlášení</a></div></body></html>",
                            cancellationToken);
                    }
                    else
                    {
                        await context.HttpContext.Response.WriteAsync("Too many login attempts. Please try again in 1 minute.", cancellationToken);
                    }
                };
            });

            var app = builder.Build();
            
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("cs-CZ")
            };

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new CookieRequestCultureProvider(),
                    new QueryStringRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                }
            };
            
			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}
			
			app.Use(async (context, next) =>
			{
				context.Response.Headers["X-Content-Type-Options"] = "nosniff";
				context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
				context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
				context.Response.Headers["Content-Security-Policy"] =
					"default-src 'self'; " +
					"script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
					"style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
					"font-src 'self' data: https://cdn.jsdelivr.net; " +
					"img-src 'self' data:; connect-src 'self'; object-src 'none'; " +
					"base-uri 'self'; form-action 'self'; frame-ancestors 'self'";
				await next();
			});
			
			app.UseStaticFiles();
			app.UseRequestLocalization(localizationOptions);
			app.UseSession();
			app.UseHttpsRedirection();

			app.UseRouting();
			app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
			

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

            // Catch-all route for non-existent routes
            app.MapFallback(context =>
            {
                // Log the fallback route if necessary
                Console.WriteLine($"Fallback triggered for: {context.Request.Path}");
				FileLogger.Instance.LogWarning($"Fallback triggered for: {context.Request.Path}");

                // Redirect to the Home/Index route with status code 302 (Found)
                context.Response.Redirect("/", permanent: false);
                return Task.CompletedTask;
            });
			FileLogger.Instance.Log("Application started.");
            //create missing directories
            Directory.CreateDirectory("logs");
            Directory.CreateDirectory("backups");
            //background checking of reservations
            var backgroundTask = app.Services.GetRequiredService<BackgroundTask>();
            backgroundTask.Start();
			//starts TelnetConsole
			Task.Run(()=>TelnetConsole.TelnetConsole.StartListener());
			//starts web app
            app.Run();

		}
	}
}