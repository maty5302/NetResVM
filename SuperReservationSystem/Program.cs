using Microsoft.AspNetCore.Authentication.Cookies;

namespace SuperReservationSystem
{
	public class Program
	{
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
                options.LoginPath = "/Home/Login";
            });
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

			app.Run();

		}
	}
}