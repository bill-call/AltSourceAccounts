using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
  public static class HttpClientExtensions
  {

    /// <summary>
    /// An extenstion method on HttpClient that sends a request to <c>client</c>, using
    /// the request type (Get, Put, Post) specified by <c>invocation</c>.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="invocation">A delegate that issues the required request type.</param>
    /// <param name="requestUrn">The URN representing the request.</param>
    /// <returns>The client's response  as a <see cref="HttpResponseMessage"/>.</returns>
    /// 
    public static Task<HttpResponseMessage> Invoke(this HttpClient client,
                                                   HttpClientHelpers.EndPointInvocation invocation,
                                                   string requestUrn,
                                                   StringContent content = null)
    {
      return invocation(client, requestUrn, content);
    }

    /// <summary>
    /// Makes a call to the specified endpoint of Accounts API, using the specified Invocation (Get, Put or Post).
    /// Content can be passed for Put and Post as <see cref="StringContent"/>.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance serving as <c>this</c>.</param>
    /// <param name="invocation">A delegate that will pass the </param>
    /// <param name="requestUrn">The URN to be appended to the Accounts API URL in order to
    /// obtain the full request URL.</param>
    /// <param name="content">The content, if any.  Only used if <c>invocation</c> is
    /// <see cref="HttpClientHelpers.Put"/> or <see cref="HttpClientHelpers.Post"/></param>
    /// <returns>An object representing the response by the Accounts API.</returns>
    /// <example>
    /// var response = InvokeManagementApi(Put, "accounts/credit/1234?sum=100");
    /// 
    /// Where "1234" is an account number and "100" is the sum to be credited to that account.
    /// </example>
    public static async Task<HttpResponseMessage> InvokeApiAsync(this HttpClient httpClient,
                                                                 HttpClientHelpers.EndPointInvocation invocation,
                                                                 string requestUrn,
                                                                 StringContent content = null)
    {
      var response = await httpClient.Invoke(invocation, requestUrn, content);

      return response;
    }
  }
}