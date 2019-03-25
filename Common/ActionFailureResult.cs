using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Common
{
  public class ActionFailureResult : ObjectResult
  {
    public ActionFailureResult(object value) : base(value)
    {
      StatusCode = (int)HttpStatusCode.BadRequest;
    }
  }
}
