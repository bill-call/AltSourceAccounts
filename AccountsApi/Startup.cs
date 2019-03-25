using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AccountsApi
{
  public class Startup
  {
    /// <summary>
    /// If a token is expired, the resource manager will return a 401 (Unauthorized) status.  Unfortunately, there
    /// are multiple reasons it might return that status, of which an expired token is just one.  To help our callers
    /// out, we trap the token validation failure and, if the reason was truly an expired token, we inject
    /// the "Token-Expired" header into the response. Now, when the client sees a 401 response, the can look for
    /// the header in order to determine whether or not it was actually due to an expired token.
    /// </summary>
    /// <param name="context">Token authentication failure context.</param>
    /// <returns>void</returns>
    private static Task OnAccessTokenAuthenticationFailed(AuthenticationFailedContext context)
    {
      if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
      {
        context.Response.Headers.Add("Token-Expired", "true");
      }

      return Task.CompletedTask;
    }

    /// <summary>
    /// This is a debug stub.  Set your breakpoint here to snoop validated tokens in the debugger,
    /// before your logic starts trying to consume them. Useful for spotting problems with
    /// claims and such.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Task OnAccessTokenValidated(TokenValidatedContext context)
    {
      return Task.CompletedTask;
    }

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvcCore()
              .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
              .AddAuthorization(options =>
                {
                  options.AddPolicy
                    (
                      AccountsApiPolicies.Admin,
                      policy => policy.RequireClaim(ApplicationClaimTypes.ROLE, AccountsApiRoles.ADMIN)
                    );

                  options.AddPolicy
                  (
                    AccountsApiPolicies.Client,
                    policy => policy.RequireClaim(ApplicationClaimTypes.ROLE, AccountsApiRoles.CLIENT)
                  );
                })
                .AddJsonFormatters();
    
      services.AddAuthentication("Bearer")
              .AddJwtBearer("Bearer", options =>
              {
                options.Authority = "http://localhost:5000";
                options.RequireHttpsMetadata = false;
            
                options.Audience = ApplicationScopes.ACCOUNTS_API;

                options.Events = new JwtBearerEvents
                {
                  OnAuthenticationFailed = OnAccessTokenAuthenticationFailed,
                  OnTokenValidated = OnAccessTokenValidated
                };
              });

      // Stop Microsoft from renaming claim types to their weird URIs.
      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();
      }

      app.UseAuthentication();
      app.UseMvc();
    }
  }
}
