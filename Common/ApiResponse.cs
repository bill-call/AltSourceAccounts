using System;
using Newtonsoft.Json;

namespace Common
{
  public class ApiResponse
  {
    public int StatusCode { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Message { get; }

    public ApiResponse(int statusCode, string message)
    {
      StatusCode = statusCode;
      Message = (message ?? String.Empty);
    }
  }
}
