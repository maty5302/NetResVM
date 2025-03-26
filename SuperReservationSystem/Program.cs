using Microsoft.AspNetCore.Authentication.Cookies;
using SimpleLogger;
using TelnetConsole;

namespace SuperReservationSystem
{
	public class Program
	{
		private static BackgroundTask _backgroundTask = new BackgroundTask();

        public static void Main(string[] args)
		{		
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews();
			builder.Services.AddSession(options =>
			{
                options.Cookie.Name = "SessionCookie";
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.IsEssential = true;
            });
			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(options =>
			{
				options.Cookie.Name = "AuthCookies";
				options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
				options.SlidingExpiration = true;
                options.LoginPath = "/Login/Index";
                options.LogoutPath = "/Login/Logout";
            });
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.ConfigureHttpsDefaults(httpsOptions =>
            //    {
            //        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            //    });
            //});

            var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}
			app.UseSession();
			app.UseHttpsRedirection();
			app.UseStaticFiles();

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
            //background checking of reservations
			_backgroundTask.Start();
			//starts TelnetConsole
			Task.Run(()=>TelnetConsole.TelnetConsole.StartListener());
			//starts web app
            app.Run();

		}
	}
}