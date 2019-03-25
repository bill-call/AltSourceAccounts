using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using EndPointInvocation = Common.HttpClientHelpers.EndPointInvocation;

namespace AccountsApi.Client
{
  internal static class ExtensionMethods
  {
    /// <summary>
    /// An extension method used to determine whether a given <c>response</c> indicates an
    /// expired Access Token. This method is specific to responses from the Accounts API,
    /// as it depends on the "Token-Expired" header being present.  This header is added
    /// to responses by the Accounts API to indicate that a specific 401 response is due
    /// to an expired Access Token.
    /// </summary>
    /// <param name="response">The response to be inspected.</param>
    /// <returns></returns>
    internal static bool IsAccessTokenExpired(this HttpResponseMessage response)
    {
      var isAccessTokenExpired = (   (response.StatusCode == HttpStatusCode.Unauthorized)
                                  && (response.Headers.Contains("Token-Expired")));

      return isAccessTokenExpired;
    }

    /// <summary>
    /// An extenstion method on HttpClient that sends a request to <c>client</c>, using
    /// the request type (Get, Put, Post) specified by <c>invocation</c>.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="invocation">A delegate that issues the required request type.</param>
    /// <param name="requestUrn">The URN representing the request.</param>
    /// <param name="content">Optional content; used only by Put and Post.</param>
    /// <returns>The client's response  as a <see cref="HttpResponseMessage"/>.</returns>
    /// 
    internal static Task<HttpResponseMessage> Invoke(this HttpClient client,
                                                     EndPointInvocation invocation,
                                                     string requestUrn,
                                                     StringContent content = null)
    {
      return invocation(client, requestUrn, content);
    }
  }
}
