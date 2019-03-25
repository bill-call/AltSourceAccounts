using System.Collections.Generic;
using Common;
using IdentityModel;

namespace AccountsApi.Controllers.Common
{
  //[SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
  //[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
  public class UserInfo
  {
    public string PreferredUsername { get; private set; }
    public string AccountsUsername { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Role { get; private set; }

    public UserInfo(IDictionary<string, string> claimsMap)
    {
      PreferredUsername = claimsMap[JwtClaimTypes.PreferredUserName];
      AccountsUsername = claimsMap[ApplicationClaimTypes.ACCOUNTS_USERNAME];
      FirstName = claimsMap[JwtClaimTypes.GivenName];
      LastName = claimsMap[JwtClaimTypes.FamilyName];
      Role = claimsMap[JwtClaimTypes.Role];
    }
  }
}
