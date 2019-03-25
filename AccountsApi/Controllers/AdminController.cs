using System.ComponentModel.DataAnnotations;
using AccountsApi.Controllers.Common;
using AccountsLib;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository = AccountsLib.Repository;

namespace AccountsApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [ValidateModel]
  public class AdminController : ControllerBase
  {
    private static readonly Repository Repository = Environment.AccountsRepository;

    // POST: api/Accounts/Create/2324
    [Authorize(Policy = AccountsApiPolicies.Admin)]
    [HttpPost("Create/{clientId}", Name = "Post_AccountCreate")]
    public ActionResult<string> Create([Required, FromRoute] string clientId)
    {
      Account newAccount;

      try
      {
        UserInputValidation.ValidateClientId(clientId);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        using (var session = Logic.Login(Repository, username))
        {
          newAccount = Logic.CreateAccount(session, clientId);
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return newAccount.AccountNumber;
    }

    //---------------------------------------------------------------------------------------------

    // POST: api/Accounts/Create/Client
    [Authorize(Policy = AccountsApiPolicies.Admin)]
    [HttpPost("Create/Client", Name = "Post_AccountCreateClient")]
    public ActionResult<string> CreateClient([Required, FromBody] AltSourceNewClientDto input)
    {
      AccountsLib.Client newClient;

      try
      {
        UserInputValidation.ValidateUsername(input.Username);
        UserInputValidation.ValidateName(input.FirstName);
        UserInputValidation.ValidateName(input.LastName);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        using (var session = Logic.Login(Repository, username))
        {
          newClient = Logic.CreateClient(session, input.Username, input.FirstName, input.LastName);
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return newClient?.ClientId;
    }

    //---------------------------------------------------------------------------------------------

    // POST: api/Accounts/Create/User/Bob
    [Authorize(Policy = AccountsApiPolicies.Admin)]
    [HttpPost("Create/User/{newUsername}", Name = "Post_AccountCreateUser")]
    public ActionResult<bool> CreateUser([Required, FromRoute] string newUsername)
    {
      UserInputValidation.ValidateUsername(newUsername);

      var userInfo = User.GetUserInfo();
      var username = userInfo.AccountsUsername;

      using (var session = Logic.Login(Repository, username))
      {
        // ReSharper disable once UnusedVariable
        var newUser = Logic.CreateUser(session, newUsername);
      }

      return true;
    }
  }
}
