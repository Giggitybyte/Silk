using AspNet.Security.OAuth.Discord;
using Blazored.Toast;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silk.Core.Data;
using Silk.Dashboard.Services;

namespace Silk.Dashboard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddBlazoredToast();

            services.AddHttpContextAccessor();

            // TODO: Add Research/Add other protections against token scraping/stealing
            services.AddDataProtection();

            services.AddScoped<DiscordRestClientService>();

            services.AddDbContext<GuildContext>(o =>
                o.UseNpgsql(Configuration.GetConnectionString("dbConnection")));

            services.AddMediatR(typeof(GuildContext));

            services.AddAuthentication(opt =>
                {
                    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
                })
                .AddDiscord(opt =>
                {
                    opt.ClientId = Configuration["Discord:AppId"];
                    opt.ClientSecret = Configuration["Discord:AppSecret"];

                    opt.CallbackPath = DiscordAuthenticationDefaults.CallbackPath;

                    /*opt.Events.OnCreatingTicket = context =>
                    {
                        context.
                    };*/

                    opt.Scope.Add("guilds");

                    opt.UsePkce = true;
                    opt.SaveTokens = true;
                })
                .AddCookie();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}