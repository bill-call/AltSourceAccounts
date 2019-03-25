using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Util
{
  public static class IdentityResultExtensionMethods
  {
    public static string CompileErrorMessage(this IdentityResult result)
    {
      var msg = ((result.Errors.Select(e => $"{e.Code}: {e.Description}.")).FirstOrDefault() ?? String.Empty);

      return msg;
    }
  }
}