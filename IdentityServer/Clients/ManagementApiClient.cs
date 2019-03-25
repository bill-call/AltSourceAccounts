using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace IdentityServer.Clients
{
  public class ManagementApiClient : HttpClient
  {
    protected static readonly HttpClientHelpers.EndPointInvocation Post = HttpClientHelpers.Post;

    /// <summary>
    /// Builds a client proxy to simplify invoking ManagementApi methods.
    /// </summary>
    /// <param name="baseAddress">The base URL for the target Management API.</param>
    /// <param name="accessToken">A valid Access Token in the form of a JWT Bearer Token.</param>
    public ManagementApiClient(Uri baseAddress, string accessToken)
    {
      BaseAddress = baseAddress;

      this.SetBearerToken(accessToken);
    }

    /// <summary>
    /// Creates a new user on the Identity Server, with a role of Client. Users created with this
    /// method can be used to query/credit/debit the accounts owned by this user, but they cannot
    /// create new users or otherwise modify Identity data.
    /// </summary>
    /// <param name="username">The new user's username; legal values constrained by the IdentityServer settings.</param>
    /// <param name="password">The new user's password; legal values constrained by the IdentityServer settings.</param>
    /// <param name="firstName">The new user's given name. </param>
    /// <param name="lastName">The new user's family name.</param>
    /// <returns></returns>
    public async Task CreateUser(string username, string password, string firstName, string lastName)
    {
      var newUser = new AltSourceNewUserDto(username, password, firstName, lastName, false);
      var jsonNewUser = JsonConvert.SerializeObject(newUser);
      var content = new StringContent(jsonNewUser, Encoding.UTF8, "application/json");

      var response = await this.InvokeApiAsync(Post, $"api/manage/user/create", content);

      if (!response.IsSuccessStatusCode)
      {
        var msg = response.Content.ReadAsStringAsync();

        throw new HttpRequestException($"{response.ReasonPhrase} - {msg.Result}");
      }
    }
  }
}
