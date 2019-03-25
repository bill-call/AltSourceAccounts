using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace AccountsApi.Client
{
  public class AccountsApiClient : HttpClient
  {
    protected static readonly HttpClientHelpers.EndPointInvocation Get = HttpClientHelpers.Get;
    protected static readonly HttpClientHelpers.EndPointInvocation Put = HttpClientHelpers.Put;
    protected static readonly HttpClientHelpers.EndPointInvocation Post = HttpClientHelpers.Post;

    /// <summary>
    /// Builds a client proxy to simplify invoking AccountApi methods.
    /// </summary>
    /// <param name="baseAddress">The base URL for the target Accounts API.</param>
    /// <param name="accessToken">A valid Access Token in the form of a JWT Bearer Token.</param>
    public AccountsApiClient(Uri baseAddress, string accessToken)
    {
      BaseAddress = baseAddress;

      this.SetBearerToken(accessToken);
    }

    /// <summary>
    /// Get the balance of an account from the Accounts API.
    /// </summary>
    /// <param name="accountNumber">The account to be queried.</param>
    /// <returns>The account balance as a double.</returns>
    /// 
    public async Task<double> GetAccountBalance(string accountNumber)
    {
      UserInputValidation.ValidateAccountNumber(accountNumber);

      var response = await this.InvokeApiAsync(Get, $"api/accounts/balance/{accountNumber}");
      var content = await response.TryGetResponseContent();
      var balance = Double.Parse(content);

      return balance;
    }

    /// <summary>
    /// Get the history of an account from the Accounts API.
    /// </summary>
    /// <param name="accountNumber">The account to be queried.</param>
    /// <returns>The account history as list of signed doubles.</returns>
    ///
    public async Task<AccountHistory> GetAccountHistory(string accountNumber)
    {
      UserInputValidation.ValidateAccountNumber(accountNumber);

      var response = await this.InvokeApiAsync(Get, $"api/accounts/history/{accountNumber}");
      var content = await response.TryGetResponseContent();
      var history = JsonConvert.DeserializeObject<AccountHistory>(content);

      return history;
    }

    /// <summary>
    /// Call the Accounts API to credit an account with some amount.
    /// </summary>
    /// <param name="accountNumber">The account to be modified.</param>
    /// <param name="amount">The (unsigned) amount to be credited to the account.</param>
    /// <param name="memo">A short note to be attached to this transaction.</param>
    /// <returns>The new balance for the modified account.</returns>
    ///
    public async Task<double> CreditAccount(string accountNumber, double amount, string memo)
    {
      UserInputValidation.ValidateAccountNumber(accountNumber);
      UserInputValidation.ValidateAmount(amount);
      UserInputValidation.ValidateMemo(memo);

      var queryString = $"api/accounts/credit/{accountNumber}?sum={Math.Abs(amount)}";

      if (memo != null)
      {
        queryString += $"&memo=\"{memo}\"";
      }

      var response = await this.InvokeApiAsync(Post, queryString);
      var content = await response.TryGetResponseContent();
      var balance = Double.Parse(content);

      return balance;
    }

    /// <summary>
    /// Call the Accounts API to debit an account by some amount.
    /// Note that <c>amount</c> should be unsigned, and that only it's absolute value
    /// will be taken in any event.
    /// </summary>
    /// <param name="accountNumber">The account to be modified.</param>
    /// <param name="amount">The (unsigned) amount to be debited from the account.</param>
    /// <param name="memo">A short note to be attached to this transaction.</param>
    /// <returns>The new balance for the modified account.</returns>
    ///
    public async Task<double> DebitAccount(string accountNumber, double amount, string memo)
    {
      UserInputValidation.ValidateAccountNumber(accountNumber);
      UserInputValidation.ValidateAmount(amount);
      UserInputValidation.ValidateMemo(memo);

      var queryString = $"api/accounts/debit/{accountNumber}?sum={Math.Abs(amount)}";

      if (memo != null)
      {
        queryString += $"&memo=\"{memo}\"";
      }

      var response = await this.InvokeApiAsync(Post, queryString);
      var content = await response.TryGetResponseContent();
      var balance = Double.Parse(content);

      return balance;
    }

    /// <summary>
    /// Creates a new account bound to the client specified by <c>clientId</c>.
    /// </summary>
    /// <param name="clientId">The clientId of the owner of the account be modified.</param>
    /// <returns>The starting balance for the new account.</returns>
    ///
    public async Task<string> CreateAccount(string clientId)
    {
      UserInputValidation.ValidateClientId(clientId);

      var response = await this.InvokeApiAsync(Post, $"api/admin/create/{clientId}");
      var accountId = await response.TryGetResponseContent();

      return accountId;
    }

    /// <summary>
    /// Creates a new user in the Accounts database. The new user must be linked to a corresponding
    /// Identity on the Identity Server by setting the <see cref="ApplicationClaimTypes.ACCOUNTS_USERNAME"/>
    /// claim for that Identity
    /// to <c>username</c>
    /// </summary>
    /// <param name="username">
    /// The username to be assigned to the new User.  Must match the value of the
    /// <see cref="ApplicationClaimTypes.ACCOUNTS_USERNAME"/> claim type on the corresponding Identity
    /// on the Identity Server.
    /// </param>
    /// <returns>void</returns>
    public async Task<bool> CreateUser(string username)
    {
      UserInputValidation.ValidateUsername(username);

      var response = await this.InvokeApiAsync(Post, $"api/admin/create/user/{username}");

      return (response.IsSuccessStatusCode);
    }

    public async Task<string> CreateClient(string username, string firstName, string lastName)
    {
      UserInputValidation.ValidateUsername(username);
      UserInputValidation.ValidateName(firstName);
      UserInputValidation.ValidateName(lastName);

      var newClient = new AltSourceNewClientDto(username, firstName, lastName);
      var jsonNewUser = JsonConvert.SerializeObject(newClient);
      var content = new StringContent(jsonNewUser, Encoding.UTF8, "application/json");
      var response = await this.InvokeApiAsync(Post, @"api/admin/create/client", content);
      var newClientId = await response.TryGetResponseContent();

      return newClientId;
    }
  }
}
