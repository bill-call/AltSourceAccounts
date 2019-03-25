using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common;
using IdentityModel;
using IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Util
{
  internal static class Constants
  {
    public static readonly string AltSourceDefaultPassword = "Pass123$";
  }

  internal class UserManagementException : Exception
  {
    public UserManagementException(string message) : base(message) {}
  }

  internal static class Users
  {
    public static async Task<IdentityResult> CreateAltSourceUser(UserManager<ApplicationUser> userManager,
                                                                 string username,
                                                                 string password,
                                                                 string firstName,
                                                                 string lastName,
                                                                 string address,
                                                                 bool isAdmin)
    {
      var isDuplicateUser = (userManager.FindByNameAsync(username).Result != null);

      if (isDuplicateUser)
      {
        return IdentityResult.Failed(
          new IdentityError
          {
            Code = "DupUsr",
            Description = $"User '{username}' already exists."
          }
        );
      }

      var newUser = new ApplicationUser
      {
        UserName = username
      };

      var result = userManager.CreateAsync(newUser, password).Result;

      if (!result.Succeeded)
      {
        throw new Exception(result.Errors.First().Description);
      }

      result = userManager.AddClaimsAsync(newUser, new Claim[]
      {
        new Claim(JwtClaimTypes.Name, $"{firstName} {lastName}"),
        new Claim(JwtClaimTypes.GivenName, firstName),
        new Claim(JwtClaimTypes.FamilyName, lastName),
        new Claim(JwtClaimTypes.Email, $"{firstName}{lastName}@email.com"),
        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
        new Claim(JwtClaimTypes.WebSite, $"http://{firstName}.com"),

        new Claim(JwtClaimTypes.Address,
                  @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }",
                  IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json),

        new Claim(ApplicationClaimTypes.ACCOUNTS_USERNAME, newUser.UserName)

      }).Result;

      if (!result.Succeeded)
      {
        throw new UserManagementException(result.Errors.First().Description);
      }

      await userManager.AddToRoleAsync(newUser, (isAdmin ? AccountsApiRoles.ADMIN : AccountsApiRoles.CLIENT));

      Console.WriteLine($"User '{username}' created.");

      return result;
    }
  }
}
