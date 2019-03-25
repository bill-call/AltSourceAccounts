using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Common;
using IdentityModel;

namespace AccountsApi.Controllers.Common
{
  internal static class ClaimsPrincipalExtensionMethods
  {
    private static readonly ISet<string> TargetClaimTypesSet = new HashSet<string>(
      new[]
      {
        JwtClaimTypes.PreferredUserName,
        ApplicationClaimTypes.ACCOUNTS_USERNAME,
        JwtClaimTypes.GivenName,
        JwtClaimTypes.FamilyName,
        JwtClaimTypes.Role
      }
    );

    private static IDictionary<string, string> GetUserClaims(ClaimsPrincipal user)
    {
      // TODO: This is not a good idea; Claim Types can appear more than once, which will cause an exception when
      // TODO: building the dictionary.
      var claimsMap = user.Claims.Where(c => TargetClaimTypesSet.Contains(c.Type)).ToDictionary(c => c.Type, c => c.Value);

      return claimsMap;
    }

    public static UserInfo GetUserInfo(this ClaimsPrincipal user)
    {
      var claims = GetUserClaims(user);
      var userInfo = new UserInfo(claims);

      return userInfo;
    }
  }
}