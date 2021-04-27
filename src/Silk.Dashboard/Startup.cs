using System;
using AspNet.Security.OAuth.Discord;
using Blazored.Toast;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silk.Core.Data;
using Silk.Dashboard.Models;
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
            services.AddDataProtection();
            
            services.AddHttpClient();
            
            services.AddSingleton<DashboardTokenStorageService>();
            services.AddScoped<DiscordRestClientService>();

            services.AddDbContext<GuildContext>(o =>
                o.UseNpgsql(Configuration.GetConnectionString("core")));

            services.AddMediatR(typeof(GuildContext));

            services.AddAuthentication(opt =>
                {
                    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
                })
                .AddDiscord(opt =>
                {
                    /* Todo: Change UserSecrets Keys to "ClientId and ClientSecret" */
                    opt.ClientId = Configuration["Discord:AppId"];
                    opt.ClientSecret = Configuration["Discord:AppSecret"];

                    opt.CallbackPath = DiscordAuthenticationDefaults.CallbackPath;

                    opt.Events.OnCreatingTicket = async (OAuthCreatingTicketContext context) =>
                    {
                        var tokenStorageService = context.HttpContext.RequestServices
                            .GetRequiredService<DashboardTokenStorageService>();
                        
                        /* Todo: Check whether need to use UTC values for Token Expiry from Response */
                        var dateTimeOffset = DateTimeOffset.UtcNow.Add(context.ExpiresIn.Value);
                        tokenStorageService.SetToken(new DiscordOAuthToken(context.AccessToken, context.RefreshToken, dateTimeOffset));
                    };
                    
                    opt.Scope.Add("guilds");

                    opt.UsePkce = true;
                    opt.SaveTokens = true;
                })
                .AddCookie(opts =>
                {
                });
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