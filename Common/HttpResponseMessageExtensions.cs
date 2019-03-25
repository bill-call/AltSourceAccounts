using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Common
{
  public static class HttpResponseMessageExtensions
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
    public static bool IsAccessTokenExpired(this HttpResponseMessage response)
    {
      var isAccessTokenExpired = (   (response.StatusCode == HttpStatusCode.Unauthorized)
                                  && (response.Headers.Contains("Token-Expired")));

      return isAccessTokenExpired;
    }

    /// <summary>
    /// Extracts the content of an <c>HttpResponseMessage</c> as a string.
    /// </summary>
    /// <param name="response">The message from which to extract content.</param>
    /// <returns>The string representation of the message content.</returns>
    /// 
    public static async Task<string> TryGetResponseContent(this HttpResponseMessage response)
    {
      string content;

      if (response.IsSuccessStatusCode)
      {
        content = await response.Content.ReadAsStringAsync();
      }
      else if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        var json = await response.Content.ReadAsStringAsync();
        var obj = JsonConvert.DeserializeObject<ApiResponse>(json);

        throw new HttpRequestException(obj.Message);
      }
      else
      {
        var errorMessage = $"Request failed - reason: {(int)response.StatusCode} - {response.ReasonPhrase}";

        throw new HttpRequestException(errorMessage);
      }

      return content;
    }
  }
}
