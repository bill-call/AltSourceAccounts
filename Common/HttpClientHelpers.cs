using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
  public static class HttpClientHelpers
  {
    /// <summary>
    /// A delegate used to invoke HTTP endpoints in a generic way. Clever, but maybe not a good idea?
    /// </summary>
    /// <param name="client">Proxy for the HTTP listener to target.</param>
    /// <param name="endPoint">The URL to send the request to.</param>
    /// <param name="content">String-serialized (typically JSON) content to be added to the request body.</param>
    /// <returns></returns>
    public delegate Task<HttpResponseMessage> EndPointInvocation(HttpClient client,
                                                                 string endPoint,
                                                                 StringContent content = null);

    /// <summary>
    /// Submits a request to <c>client</c> as an HTTP GET operation.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="requestUrn">The request to be submitted.</param>
    /// <returns>The client's response  as a <see cref="HttpResponseMessage"/>.</returns>
    /// 
    public static async Task<HttpResponseMessage> Get(HttpClient client,
                                                      string requestUrn,
                                                      // Do not add to docs. Here to satisfy delegate signature only.
                                                      // ReSharper disable once UnusedParameter.Local
                                                      // ReSharper disable once InvalidXmlDocComment
                                                      StringContent content = null)
    {
      var response = await client.GetAsync(requestUrn);

      return response;
    }

    /// <summary>
    /// Submits a request to <c>client</c> as an HTTP PUT operation.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="requestUrn">The request to be submitted.</param>
    /// <param name="content">The content, if any</param>
    /// <returns>The client's response  as a <see cref="HttpResponseMessage"/>.</returns>
    /// 
    public static async Task<HttpResponseMessage> Put(HttpClient client,
                                                     string requestUrn,
                                                     StringContent content = null)
    {
      var response = await client.PutAsync(requestUrn, content);

      return response;
    }

    /// <summary>
    /// Submits a request to <c>client</c> as an HTTP POST operation.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="requestUrn">The request to be submitted.</param>
    /// <param name="content">The content, if any</param>
    /// <returns>The client's response  as a <see cref="HttpResponseMessage"/>.</returns>
    /// 
    public static async Task<HttpResponseMessage> Post(HttpClient client,
                                                       string requestUrn,
                                                       StringContent content = null)
    {
      var response = await client.PostAsync(requestUrn, content);

      return response;
    }
  }
}