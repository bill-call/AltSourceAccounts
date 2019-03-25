using System.Linq;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsApi.Controllers
{
  [Route("identity")]
  [Authorize]
  [ValidateModel]
  public class IdentityController : ControllerBase
  {
    [HttpGet]
    public IActionResult Get()
    {
      return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
    }
  }
}
