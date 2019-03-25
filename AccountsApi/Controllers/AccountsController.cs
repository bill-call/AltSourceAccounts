using System.ComponentModel.DataAnnotations;
using System.Linq;
using AccountsApi.Client;
using AccountsApi.Controllers.Common;
using AccountsLib;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Environment = AccountsApi.Controllers.Common.Environment;
using ETransactionType = AccountsApi.Client.ETransactionType;
using Repository = AccountsLib.Repository;
using Transaction = AccountsApi.Client.Transaction;

namespace AccountsApi.Controllers
{
  [Authorize]
  [ApiController]
  [Produces("application/json")]
  [Route("api/[controller]")]
  [ValidateModel]
  public class AccountsController : ControllerBase
  {
    private static readonly Repository Repository = Environment.AccountsRepository;

    // GET: api/Accounts/Balance/5}
    [Authorize(Policy = AccountsApiPolicies.Client)]
    [HttpGet("Balance/{accountNumber}", Name = "Get_AccountBalance")]
    public ActionResult<double> GetBalance([Required, FromRoute] string accountNumber)
    {
      double balance;

      try
      {
        UserInputValidation.ValidateAccountNumber(accountNumber);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        using (var session = Logic.Login(Repository, username))
        {
          balance = Logic.GetBalance(session, accountNumber);
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return balance;
    }

    //---------------------------------------------------------------------------------------------

    // GET: api/Accounts/History/5
    [HttpGet("History/{accountNumber}", Name = "Get_AccountHistory")]
    [Authorize(Policy = AccountsApiPolicies.Client)]
    public ActionResult<AccountHistory> GetHistory([Required, FromRoute] string accountNumber)
    {
      AccountHistory history;

      try
      {
        UserInputValidation.ValidateAccountNumber(accountNumber);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        using (var session = Logic.Login(Repository, username))
        {
          history = new AccountHistory
          {
            StartingBalance = 0.0,
            FinalBalance = 0.0,

            History = Logic.GetTransactionHistory(session, accountNumber)
                           .Select(t => new Transaction(t))
                           .ToList()
          };

          history.FinalBalance +=
            history.History.Sum(t => (t.Action == ETransactionType.Credit) ? t.Amount : (-t.Amount));
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return history;
    }

    //---------------------------------------------------------------------------------------------

    // PUT: api/Accounts/Credit/5?sum=100&memo="some text"
    [Authorize(Policy = AccountsApiPolicies.Client)]
    [HttpPost("Credit/{accountNumber}", Name = "Post_AccountCredit")]
    public ActionResult<double> Credit([Required, FromRoute] string accountNumber, 
                                       [BindRequired, FromQuery] double sum,
                                       [Required, FromQuery] string memo)
    {
      double balance;

      try
      {
        memo = Util.Dequotify(memo);

        UserInputValidation.ValidateAccountNumber(accountNumber);
        UserInputValidation.ValidateAmount(sum);
        UserInputValidation.ValidateMemo(memo);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        sum = Util.TruncateMoneyValue(sum);

        using (var session = Logic.Login(Repository, username))
        {
          Logic.ExecuteTransaction(session, accountNumber, AccountsLib.ETransactionType.Credit, sum, memo);

          balance = Logic.GetBalance(session, accountNumber);
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return balance;
    }

    //---------------------------------------------------------------------------------------------

    // PUT: api/Accounts/5?sum=100&memo="some text"
    [Authorize(Policy = AccountsApiPolicies.Client)]
    [HttpPost("Debit/{accountNumber}", Name = "Post_AccountDebit")]
    public ActionResult<double> Debit([Required, FromRoute] string accountNumber,
                                      [BindRequired, FromQuery] double sum,
                                      [FromQuery] string memo)
    {
      double balance;

      try
      {
        memo = Util.Dequotify(memo);

        UserInputValidation.ValidateAccountNumber(accountNumber);
        UserInputValidation.ValidateAmount(sum);
        UserInputValidation.ValidateMemo(memo);

        var userInfo = User.GetUserInfo();
        var username = userInfo.AccountsUsername;

        sum = Util.TruncateMoneyValue(sum);

        using (var session = Logic.Login(Repository, username))
        {
          Logic.ExecuteTransaction(session, accountNumber, AccountsLib.ETransactionType.Debit, sum, memo);

          balance = Logic.GetBalance(session, accountNumber);
        }
      }
      catch (AccountsLibException ex)
      {
        return new ActionFailureResult(new ApiResponse(500, ex.Message));
      }

      return balance;
    }
  }
}
