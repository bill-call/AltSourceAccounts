// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Common;
using IdentityServer.Data;
using IdentityServer.Models;
using IdentityServer.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer
{
  public class SeedData
  {
    /// <summary>
    /// Seeds the database with default Users and Roles; binds either the Admin or Cient role to
    /// each seeded user, as appropriate.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="sqlite"></param>
    /// <returns></returns>
    public static async Task EnsureSeedData(IServiceCollection services, SqliteConnection sqlite)
    {
      using (var serviceProvider = services.BuildServiceProvider())
      {
        using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
          var context = scope.ServiceProvider.GetService<ApplicationDbContext>();

          context.Database.Migrate();

          var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
          var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

          try
          {
            await SeedDefaultRoles(roleManager);
            await SeedDefaultUsers(userManager);
          }
          catch (UserManagementException ex)
          {
            Console.WriteLine(ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// Adds the default Role set to the database.
    /// </summary>
    /// <param name="roleManager">A <see cref="RoleManager&lt;IdentityRole&gt;"/> instance.</param>
    /// <returns>void</returns>
    protected static async Task SeedDefaultRoles(RoleManager<IdentityRole> roleManager)
    {
      var accountsApiAdminRole = new IdentityRole(AccountsApiRoles.ADMIN);
      var accountsApiClientRole = new IdentityRole(AccountsApiRoles.CLIENT);

      await roleManager.CreateAsync(accountsApiAdminRole);
      await roleManager.CreateAsync(accountsApiClientRole);

      // await roleMgr.AddClaimAsync(accountsApiClientRole, new Claim("permission", "create.foo"));
    }

    /// <summary>
    /// Adds the default Users to the database.
    /// </summary>
    /// <param name="userManager">A <see cref="UserManager&lt;ApplicationUser&gt;"/> instance.</param>
    /// <returns>void</returns>
    protected static async Task SeedDefaultUsers(UserManager<ApplicationUser> userManager)
    {
      await Users.CreateAltSourceUser(userManager,
                                      "alice",
                                      Constants.AltSourceDefaultPassword,
                                      "Alice",
                                      "Smith",
                                      @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }",
                                      true);

      await Users.CreateAltSourceUser(userManager,
                                      "bob",
                                      Constants.AltSourceDefaultPassword,
                                      "Bob",
                                      "Smith",
                                      @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }",
                                      true);
    }
  }
}
