using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AccountsApi.Client;
using Common;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AccountsCli.Util
{
  public class Actions
  {
    private readonly Uri _identityServerUrl;
    private readonly Uri _accountsApiUrl;
    private readonly string _clientSecret;

    private static readonly ITokenManager TokenManager = new DefaultTokenManager();
    private static readonly HttpClientHelpers.EndPointInvocation Get = HttpClientHelpers.Get;

    private static DiscoveryResponse DiscoveryDocument { get; set; }

    public delegate Task<(string, string)> UserCredentialsCallback();

    /// <summary>
    /// Constructor for passing configuration data.
    /// </summary>
    /// <param name="identityServerUrl">The URL of the Identity Authority.</param>
    /// <param name="accountsApiUrl">The URL of the Accounts Web API.</param>
    /// <param name="clientSecret">The Client Secret used by this client to authenticate itself to the Identity Authority.</param>
    public Actions(string identityServerUrl, string accountsApiUrl, string clientSecret)
    {
      _clientSecret = clientSecret;
      _identityServerUrl = new Uri($"{identityServerUrl}");
      _accountsApiUrl = new Uri($"{accountsApiUrl}");
    }

  /// <summary>
  /// Retrieve the discovery document from the Identity Server's discovery endpoint.
  /// </summary>
  /// <param name="uri">The URI of the Identity Server to be queried.</param>
  /// <returns>A <c>DiscoveryResponse</c> instance, representing the discovery document as a C# class.</returns>
  /// 
  private async Task<DiscoveryResponse> GetDiscoveryDocumentAsync(Uri uri)
    {
      var disco = DiscoveryDocument;

      if (disco == null)
      {
        // discover endpoints from metadata
        using (var client = new HttpClient {BaseAddress = uri})
        {
          disco = await client.GetDiscoveryDocumentAsync();
        }

        if (disco.IsError)
        {
          Console.WriteLine(disco.Error);
        }
        else
        {
          DiscoveryDocument = disco;
        }
      }

      return disco;
    }

    /// <summary>
    /// Obtains an Access Token from an Identity Server using username/password credentials that were
    /// known or obtained by the client.  To be used by native clients only.
    /// </summary>
    /// <param name="request">Represents the user's login credentials.</param>
    /// <returns>
    /// A <c>TokenResponse</c> instance containing the user's Access Token and (if supported by
    /// this Identity Server for this Client) a Refresh Token.
    /// </returns>
    /// 
    private async Task<TokenResponse> RequestAccessTokenAsync(PasswordTokenRequest request)
    {
      TokenResponse tokenResponse;

      using (var client = new HttpClient())
      {
        // Use this requestUrl for clients that cannot host a login page and must obtain (or just know) the credentials themselves.
        tokenResponse = await client.RequestPasswordTokenAsync(request);
      }

      // if (tokenResponse.IsError)
      // {
      //   Console.WriteLine(tokenResponse.Error);
      // }
      // else
      // {
      //   Console.WriteLine(tokenResponse.Json);
      //   Console.WriteLine("\n\n");
      // }

      return tokenResponse;
    }

    /// <summary>
    /// Obtains an Access Token from an Identity Server using a refresh token obtained previously.
    /// </summary>
    /// <param name="request">Represents the user's Refresh Token.</param>
    /// <returns>
    /// A <c>TokenResponse</c> instance containing the user's new Access Token.
    /// </returns>
    /// 
    private async Task<TokenResponse> RequestRefreshTokenAsync(RefreshTokenRequest request)
    {
      TokenResponse tokenResponse;

      using (var client = new HttpClient())
      {
        // Use this requestUrl for clients that cannot host a login page and must obtain (or just know) the credentials themselves.
        tokenResponse = await client.RequestRefreshTokenAsync(request);
      }

      if (tokenResponse.IsError)
      {
        Console.WriteLine(tokenResponse.Error);
      }
      else
      {
        Console.WriteLine(tokenResponse.Json);
        Console.WriteLine("\n\n");
      }

      return tokenResponse;
    }

    /// <summary>
    /// Makes an object that represents a requestUrl, to an Identity Server, for access by this
    /// API on behalf of a User, as identified by the User's username/password credentials.
    /// </summary>
    /// <param name="tokenEndpoint">The URI of the target Identity Server's Token EndPoint.</param>
    /// <param name="username">The Username of the requesting User.</param>
    /// <param name="password">the Password of the requesting User.</param>
    /// <returns>A new requestUrl object.</returns>
    /// 
    private PasswordTokenRequest MakeApiAccessTokenRequest(string tokenEndpoint,
                                                           string username,
                                                           string password)
    {
      var request = new PasswordTokenRequest
      {
        Address = tokenEndpoint,
        ClientId = "AccountsCli.ro",
        ClientSecret = _clientSecret,
        Scope = $"{ApplicationScopes.USER_MANAGEMENT_API } {ApplicationScopes.ACCOUNTS_API} offline_access openid profile",

        UserName = username,
        Password = password
      };

      return request;
    }

    /// <summary>
    /// Makes an object that represents a requestUrl, to an Identity Server, for a new Access Token.
    /// A Refresh Token is provided as part of the requestUrl, as a means of authentication.
    /// </summary>
    /// <param name="tokenEndpoint">The Token EndPoint of the target Identity Server.</param>
    /// <param name="refreshToken"></param>
    /// <returns>A new requestUrl object.</returns>
    ///
    private RefreshTokenRequest MakeAccountsApiRefreshTokenRequest(string tokenEndpoint, string refreshToken)
    {
      var request = new RefreshTokenRequest
      {
        Address = tokenEndpoint,
        ClientId = "AccountsCli.ro",
        ClientSecret = _clientSecret,
        Scope = "accounts_api offline_access",
        RefreshToken = refreshToken
      };
  
      return request;
    }

    /// <summary>
    /// Makes a call to the specified endpoint of Accounts API, using the specified Invocation (Get, Put or Post).
    /// This call only supports requests that can be fully encoded in the URL.
    /// </summary>
    /// <param name="invocation">A delegate that will pass the </param>
    /// <param name="requestUrn">The URN to be appended to the Accounts API URL in order to
    /// obtain the full request URL.</param>
    /// <returns>An object representing the response by the Accounts API.</returns>
    /// <example>
    /// var response = await InvokeApiAsync(Put, "accounts/credit/1234?sum=100");
    ///
    /// Where "1234" is an account number and "100" is the sum to be credited to that account.
    /// </example>
    private async Task<HttpResponseMessage> InvokeApiAsync(HttpClientHelpers.EndPointInvocation invocation, string requestUrn)
    {
      HttpResponseMessage response;

      var accessToken = TokenManager.GetAccessTokens().AccessToken;

      using (var apiClient = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        response = await apiClient.Invoke(invocation, requestUrn);
      }

      return response;
    }

    /// <summary>
    /// Submits a request URL to the Accounts API. Contains the retry logic required in order to
    /// deal with expired Access/Refresh Tokens.
    /// </summary>
    /// <param name="invocation">The calling method (Get, Put, or Post).</param>
    /// <param name="requestUrn">The request to be submitted to the Accounts API.
    /// See: <see cref="InvokeApiAsync(HttpClientHelpers.EndPointInvocation,string)"/>
    /// </param>
    /// <param name="userCredentialsCallback">
    /// A delegate that can be invoked to obtain username/password credentials from the user.
    /// </param>
    /// <returns>The response from the Accounts API.</returns>
    ///
    private async Task<HttpResponseMessage> InvokeApiAsync(HttpClientHelpers.EndPointInvocation invocation,
                                                                  string requestUrn, 
                                                                  UserCredentialsCallback userCredentialsCallback)
    {
      // ReSharper disable once NotAccessedVariable
      AccessTokenPair accessTokens;

      //--------------------------------------------------------------------------------------------
      // Have we been authenticated?  Check the Token Manager for an Access Token. If we don't have
      // one, go get one.
      //--------------------------------------------------------------------------------------------

      // ReSharper disable once RedundantAssignment
      if ((accessTokens = TokenManager.GetAccessTokens()).IsNull())
      {
        var (username, password) = await userCredentialsCallback();

        await GetAndStoreApiAccessTokens(username, password);
      }

      // TODO handle failure

      //--------------------------------------------------------------------------------------------
      // We have an Access Token; issue the request to the Accounts API.
      //--------------------------------------------------------------------------------------------

      var response = await InvokeApiAsync(invocation, requestUrn);

      //--------------------------------------------------------------------------------------------
      // Did the request fail?
      //--------------------------------------------------------------------------------------------

      if (!response.IsSuccessStatusCode)
      {
        Console.WriteLine(response.StatusCode);

        //------------------------------------------------------------------------------------------
        // Failed. Was it because our access token timed out?
        //------------------------------------------------------------------------------------------

        if (response.IsAccessTokenExpired())
        {
          //----------------------------------------------------------------------------------------
          // Yes. Use our refresh token to get a new access token.
          //----------------------------------------------------------------------------------------

          // ReSharper disable once NotAccessedVariable
          var accessToken = await GetAndStoreApiAccessTokens();

          //----------------------------------------------------------------------------------------
          // Re-submit the original request with the new access token.
          //----------------------------------------------------------------------------------------

          response = await InvokeApiAsync(invocation, requestUrn);

          //----------------------------------------------------------------------------------------
          // Is the Accounts API still reporting an expired token?
          //----------------------------------------------------------------------------------------

          if (response.IsAccessTokenExpired())
          {
            //--------------------------------------------------------------------------------------
            // Yes. Apparently, our refresh token has expired too. At this point we must ask the
            // user to re-authenticate with their username/password. Use the caller-provided
            // callback to obtain them.
            //--------------------------------------------------------------------------------------

            var (username, password) = await userCredentialsCallback();

            //--------------------------------------------------------------------------------------
            // Try to get a new access token one more time, using the user's credentials.
            //--------------------------------------------------------------------------------------

            // ReSharper disable once RedundantAssignment
            accessToken = await GetAndStoreApiAccessTokens(username, password);

            //--------------------------------------------------------------------------------------
            // Either that worked, or it didn't. Either way, we're done here.  Return the final
            // response to the caller.
            //--------------------------------------------------------------------------------------

            response = await InvokeApiAsync(invocation, requestUrn);
          }
        }
      }

      return response;
    }

    /// <summary>
    /// Extracts the content of an <c>HttpResponseMessage</c> as a string.
    /// </summary>
    /// <param name="response">The message from which to extract content.</param>
    /// <returns>The string representation of the message content.</returns>
    /// 
    private async Task<string> TryGetResponseContent(HttpResponseMessage response)
    {
      string content = null;

      if (response.IsSuccessStatusCode)
      {
        content = await response.Content.ReadAsStringAsync();
      }
      else
      {
        Console.WriteLine(response.StatusCode);
      }

      return content;
    }

    /// <summary>
    /// Uses a Refresh Token to obtain a new Access Token and stores it in the Token Manager. 
    /// </summary>
    /// <returns>If successful, returns the new Access Token as a string. Otherwise...</returns>
    /// 
    private async Task<string> GetAndStoreApiAccessTokens()
    {
      TokenManager.ClearAccessTokens();

      var disco = await GetDiscoveryDocumentAsync(_identityServerUrl);

      var request = MakeAccountsApiRefreshTokenRequest(disco.TokenEndpoint,
                                                       TokenManager.GetAccessTokens().RefreshToken);

      var response = await RequestRefreshTokenAsync(request);

      // TODO: handle failure.

      TokenManager.SetAccessTokens(response.AccessToken, response.RefreshToken);

      return response.AccessToken;
    }

    /// <summary>
    /// Uses client-provided username/password credentials to obtain and store an Access Token and
    /// (if supported by this Identity Server for this Client) a refresh token.  The returned
    /// token(s) are stored in the global Token Manager for future reference.
    /// </summary>
    /// <param name="username">The requesting user's username.</param>
    /// <param name="password">The requesting user's password.</param>
    /// <returns>If successful, returns the new Access Token as a string. Otherwise...</returns>
    /// 
    private async Task<string> GetAndStoreApiAccessTokens(string username, string password)
    {
      TokenManager.ClearAccessTokens();

      var disco = await GetDiscoveryDocumentAsync(_identityServerUrl);

      var request = MakeApiAccessTokenRequest(disco.TokenEndpoint, username, password);

      var response = await RequestAccessTokenAsync(request);

      // TODO: Handle failure case.

      TokenManager.SetAccessTokens(response.AccessToken, response.RefreshToken);

      return response.AccessToken;
    }

    private async Task<string> GetCurrentAccessToken(UserCredentialsCallback userCredentialsCallback)
    {
      var tokens = TokenManager.GetAccessTokens();

      if (!String.IsNullOrEmpty(tokens?.AccessToken))
      {
        return tokens.AccessToken;
      }

      var (username, password) = await userCredentialsCallback();

      return await GetAndStoreApiAccessTokens(username, password);
    }

    /// <summary>
    /// We're using JWTs, not Reference Tokens, so we can't actually invalidate our
    /// Access Tokens.  We  could use this method to have Identity Server put our
    /// access token on a blacklist.
    /// </summary>
    /// <returns>void</returns>
    #pragma warning disable 1998
    private async Task RevokeCurrentAccessToken()
    {
      // This will have to do, for now.
      TokenManager.ClearAccessTokens();
    }
    #pragma warning restore 1998

    /// <summary>
    /// Users an Access Token to retrieve user-related Claims from the Identity Server's UserInfo endpoint.
    /// </summary>
    /// <returns>
    /// A <see cref="UserInfoResponse"/>. If the call is successful, as per the <c>UserInfoResponse.isError</c>
    /// property, then returned <c>UserInfoResponse</c> will contain the user-related claims for the User
    /// associated with the provided Access Token. See: <see cref="UserInfoResponse.Claims"/>.
    /// </returns>

    private async Task<UserInfoResponse> GetCurrentUserInfo()
    {
      var tokens = TokenManager.GetAccessTokens();
      var disco = await GetDiscoveryDocumentAsync(_identityServerUrl);
      
      var userInfoRequest = new UserInfoRequest
      {
        Token = tokens.AccessToken
      };

      UserInfoResponse response;

      using (var identityClient = new HttpClient {BaseAddress = new Uri(disco.UserInfoEndpoint)})
      {
        response = await identityClient.GetUserInfoAsync(userInfoRequest);
      }

      return response;
    }

    /// <summary>
    /// Obtains the claims supported by this Identity Server for this client as a JSON string.
    /// </summary>
    /// <param name="userCredentialsCallback">
    /// A callback used to obtain login credentials in the case that the current Access Token and
    /// Refresh Token (if any) are expired.
    /// </param>
    /// <returns>A JSON string representing the Claims for this API.</returns>
    /// 
    public async Task<string> GetAccountsApiClaimsAsJson(UserCredentialsCallback userCredentialsCallback)
    {
      var response = await InvokeApiAsync(Get, "identity", userCredentialsCallback);
      var content = await TryGetResponseContent(response);

      return content;
    }

    /// <summary>
    /// Get the balance of an account from the Accounts API, on behalf of a User.
    /// </summary>
    /// <param name="accountNumber">The account to be queried.</param>
    /// <param name="userCredentialsCallback">A delegate that, if invoked, returns the User's credentials.</param>
    /// <returns>The account balance as a double.</returns>
    /// 
    public async Task<double> GetAccountBalance(string accountNumber, UserCredentialsCallback userCredentialsCallback)
    {
      double balance;

      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        balance = await accountsApi.GetAccountBalance(accountNumber);
      }

      return balance;
    }

    /// <summary>
    /// Get the history of an account from the Accounts API, on behalf of a User.
    /// </summary>
    /// <param name="accountNumber">The account to be queried.</param>
    /// <param name="userCredentialsCallback">A delegate that, if invoked, returns the User's credentials.</param>
    /// <returns>The account history as list of signed doubles.</returns>
    ///
    public async Task<AccountHistory> GetAccountHistory(string accountNumber, UserCredentialsCallback userCredentialsCallback)
    {
      AccountHistory history;

      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        history = await accountsApi.GetAccountHistory(accountNumber);
      }

      return history;
    }

    /// <summary>
    /// Call the Accounts API, on behalf of a User, to credit an account with some amount.
    /// </summary>
    /// <param name="accountNumber">The account to be modified.</param>
    /// <param name="amount">The (unsigned) amount to be credited to the account.</param>
    /// <param name="memo">A short note to be attached to this transaction.</param>
    /// <param name="userCredentialsCallback">A delegate that, if invoked, returns the User's credentials.</param>
    /// <returns>The new balance for the modified account.</returns>
    ///
    public async Task<double> CreditAccount(string accountNumber, double amount, string memo,
                                            UserCredentialsCallback userCredentialsCallback)
    {
      double balance;

      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        balance = await accountsApi.CreditAccount(accountNumber, amount, memo);
      }

      return balance;
    }

    /// <summary>
    /// Call the Accounts API, on behalf of a User, to debit an account by some amount.
    /// Note that <c>amount</c> should be unsigned, and that only it's absolute value
    /// will be taken in any event.
    /// </summary>
    /// <param name="accountNumber">The account to be modified.</param>
    /// <param name="amount">The (unsigned) amount to be debited from the account.</param>
    /// <param name="memo">A short note to be attached to this transaction.</param>
    /// <param name="userCredentialsCallback">A delegate that, if invoked, returns the User's credentials.</param>
    /// <returns>The new balance for the modified account.</returns>
    ///
    public async Task<double> DebitAccount(string accountNumber, double amount, string memo,
                                           UserCredentialsCallback userCredentialsCallback)
    {
      double balance;

      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        balance = await accountsApi.DebitAccount(accountNumber, amount, memo);
      }

      return balance;
    }

    /// <summary>
    /// Used by Admin users to create a new account bound to the client specified by <c>clientId</c>.
    /// </summary>
    /// <param name="clientId">The account to be modified.</param>
    /// <param name="userCredentialsCallback">A delegate that, if invoked, returns the User's credentials.</param>
    /// <returns>The starting balance for the new account.</returns>
    ///
    public async Task<string> CreateAccount(string clientId, UserCredentialsCallback userCredentialsCallback)
    {
      string accountId;

      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        accountId = await accountsApi.CreateAccount(clientId);
      }

      return accountId;
    }

    /// <summary>
    /// Authenticate with the Accounts API as an Admin or User.
    /// </summary>
    /// <param name="username">Admin/User username.</param>
    /// <param name="password">Admin/User password.</param>
    /// <returns>A boolean indicating whether or not login was successful.</returns>
    public async Task<IDictionary<string, string>> Login(string username, string password)
    {
      await RevokeCurrentAccessToken();

      if (await GetAndStoreApiAccessTokens(username, password) == null)
      {
        throw new LoginException($"Login for '{username}' failed; username and/or password was invalid.");
      }

      var userInfoResponse = await GetCurrentUserInfo();

      if (userInfoResponse.IsError)
      {
        if (userInfoResponse.Exception != null)
        {
          throw userInfoResponse.Exception;
        }

        throw new LoginException($"Login for '{username}' failed - {userInfoResponse.HttpErrorReason}.");
      }

      // TODO: This is dangerous: the Claim Types in a Claims set are not guaranteed to be unique.
      var userInfo = userInfoResponse.Claims.ToDictionary(c => c.Type, c => c.Value);

      return userInfo;
    }

    /// <summary>
    /// Log the currently-authenticated User/Admin out of the Accounts API.  Invalidates any
    /// Access Tokens associated with the current user.
    /// </summary>
    /// <returns>A boolean indicating whether or not logout was successful.</returns>
    public async Task<bool> Logout()
    {
      await RevokeCurrentAccessToken();

      return true;
    }

    /// <summary>
    /// Creates a new Identity with the specified username, password, and name.  Also creates a corresponding
    /// User on the Accounts system and links it to the new Identity via the <see cref="ApplicationClaimTypes.ACCOUNTS_USERNAME"/>
    /// claim.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="isAdmin"></param>
    /// <param name="userCredentialsCallback"></param>
    /// <returns></returns>
    public async Task CreateUser(string username,
                                 string password,
                                 string firstName,
                                 string lastName,
                                 bool isAdmin,
                                 UserCredentialsCallback userCredentialsCallback)
    {
      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);
      var newUser = new AltSourceNewUserDto(username, password, firstName, lastName, isAdmin);
      var jsonNewUser = JsonConvert.SerializeObject(newUser);
      var content = new StringContent(jsonNewUser, Encoding.UTF8, "application/json");

      using (var identityApi = new HttpClient { BaseAddress = _identityServerUrl })
      {
        identityApi.SetBearerToken(accessToken);

        var requestUrl = (isAdmin ? "api/manage/admin/create" : "api/manage/user/create");
        var result = await identityApi.PutAsync(requestUrl, content);

        if (!result.IsSuccessStatusCode)
        {
          var msg = result.Content.ReadAsStringAsync();

          throw new HttpRequestException($"{result.ReasonPhrase} - {msg.Result}");
        }
      }

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        if (!await accountsApi.CreateUser(username))
        {
          throw new HttpRequestException($"Failed to create user '{username}' on the Accounts system.");
        }
      }
    }

    /// <summary>
    /// Create a Client with and bind it to the specified username.  This request will fail if the User
    /// specified by <c>username</c> is already bound to a Client.
    /// </summary>
    /// <param name="username">The username of the User to bind to the new Client.</param>
    /// <param name="firstName">The client's first/given name.</param>
    /// <param name="lastName">The client's last/family name.</param>
    /// <param name="userCredentialsCallback"></param>
    /// <returns></returns>
    public async Task<string> CreateClient(string username,
                                           string firstName,
                                           string lastName,
                                           UserCredentialsCallback userCredentialsCallback)
    {
      var accessToken = await GetCurrentAccessToken(userCredentialsCallback);

      using (var accountsApi = new AccountsApiClient(_accountsApiUrl, accessToken))
      {
        var clientId = await accountsApi.CreateClient(username, firstName, lastName);

        return clientId;
      }
    }
  }
}
