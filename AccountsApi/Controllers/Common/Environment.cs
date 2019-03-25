using System.Security.AccessControl;

namespace AccountsApi.Controllers.Common
{
  public static class Environment
  {
    private static readonly AccountsLib.Repository AccountsRepo = AccountsLib.Repository.Make();

    public static AccountsLib.Repository AccountsRepository => AccountsRepo;
  }
}