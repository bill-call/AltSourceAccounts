using System;
using System.Data;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer.Services.Email;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace IdentityServer
{
  public class Startup: IDisposable
  {
    // TODO Unify with AccountsCli.Startup in Common
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

    public static Task OnAccessTokenMessageReceived(MessageReceivedContext context)
    {
      return Task.CompletedTask;
    }

    public Startup(IConfiguration configuration, IHostingEnvironment environment)
    {
      Environment = environment;
      Configuration = configuration;
    }

    public IHostingEnvironment Environment { get; }
    public IConfiguration Configuration { get; }

    private SqliteConnection _sqlite;

    /// <summary>
    /// This is a bit of a hack, but opening a connection here will ensure that the in-memory
    /// Sqlite instance persists and maintains its state for as long as the service is running.
    /// </summary>
    private void AnchorInMemoryDatabase()
    {
      var connectionString = Configuration.GetConnectionString("DefaultConnection");

      _sqlite = new SqliteConnection(connectionString);
      _sqlite.Open();
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public async void ConfigureServices(IServiceCollection services)
    {
      // Ensure that our in-memory database does not lose state between connections.
      AnchorInMemoryDatabase();

      services.Configure<CookiePolicyOptions>(options =>
      {
        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
      });

      services.AddDbContext<ApplicationDbContext>(
        options => options.UseSqlite(_sqlite)
      );

      services.AddIdentity<ApplicationUser, IdentityRole>(config => { config.SignIn.RequireConfirmedEmail = false; })
              // .AddDefaultUI(UIFramework.Bootstrap4) // Disabled because it causes UI to be included as Razor class lib.
              .AddEntityFrameworkStores<ApplicationDbContext>()
              .AddDefaultTokenProviders();

      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
        .AddRazorPagesOptions(options =>
        {
          options.AllowAreas = true;
          options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
          options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
        });

      services.ConfigureApplicationCookie(options =>
      {
        options.LoginPath = $"/Identity/Account/Login";
        options.LogoutPath = $"/Identity/Account/Logout";
        options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
      });

      // using Microsoft.AspNetCore.Identity.UI.Services;
      services.AddSingleton<IEmailSender, EmailSender>();

      // Run all the migrations and seed tables with default values.
      await SeedData.EnsureSeedData(services, _sqlite);

      services.AddMvcCore()
        .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
        .AddAuthorization(options =>
        {
          options.AddPolicy
          (
            ManagementApiPolicies.Admin,
            policy => policy.RequireClaim(ApplicationClaimTypes.ROLE, AccountsApiRoles.ADMIN)
          );
        })
        .AddJsonFormatters();

      // This will add Cookie Authentication, but will still need JWT/Bearer Authentication
      var identityBuilder = services.AddIdentityServer(options =>
        {
          options.Events.RaiseErrorEvents = true;
          options.Events.RaiseInformationEvents = true;
          options.Events.RaiseFailureEvents = true;
          options.Events.RaiseSuccessEvents = true;
        })
        .AddInMemoryIdentityResources(Config.GetIdentityResources())
        .AddInMemoryApiResources(Config.GetApis())
        .AddInMemoryClients(Config.GetClients(Configuration))
        .AddAspNetIdentity<ApplicationUser>();

      // Now, add JWT/Bearer Authentication.  Without this, calls from APIs
      // won't be authenticated.
      // https://github.com/IdentityServer/IdentityServer4.AccessTokenValidation

     services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
             .AddIdentityServerAuthentication(options =>
             {
               options.Authority = Configuration["IdentityServer:Authority:Url"];
               options.ApiName = ApplicationScopes.USER_MANAGEMENT_API;
               options.RequireHttpsMetadata = false;
         
               options.Events = new JwtBearerEvents
               {
                 OnAuthenticationFailed = OnAccessTokenAuthenticationFailed,
                 OnTokenValidated = OnAccessTokenValidated,
                 OnMessageReceived = OnAccessTokenMessageReceived
               };
             });

      if (Environment.IsDevelopment())
      {
        identityBuilder.AddDeveloperSigningCredential();
      }
      else
      {
        throw new Exception("need to configure key material");
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        //// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();
      }

      app.UseStaticFiles();
      app.UseCookiePolicy();

      app.UseIdentityServer();  // Invokes UseAuthentication(), among other things.

      app.UseMvc(routes =>
      {
        routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=Index}/{id?}");
      });
    }

    public void Dispose()
    {
      if (_sqlite.State != ConnectionState.Closed)
      {
        try
        {
          _sqlite.Close();
        }
        finally
        {
          _sqlite = null;
        }
      }
    }
  }
}
