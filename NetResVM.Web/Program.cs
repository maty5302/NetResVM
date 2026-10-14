using ApiCisco;
using ApiEVE;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using DataLayer;
using DataLayer.Interface;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using SimpleLogger;
using System.Globalization;

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
            
            app.UseRequestLocalization(localizationOptions);
            
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
				await next();
			});
			
			app.UseStaticFiles();
			app.UseSession();
			app.UseHttpsRedirection();

			app.UseRouting();
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