using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Common;
using IdentityServer.Areas.Identity.Pages.Account;
using IdentityServer.Models;
using IdentityServer.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IdentityServer.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [ValidateModel]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  [ProducesResponseType(200)]
  [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public class ManageController : ControllerBase
  {
    private readonly ILogger _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageController(UserManager<ApplicationUser> userManager,
                            ILogger<ManageController> logger)
    {
      _logger = logger;
      _userManager = userManager;
    }

    // POST: api/manage/user/create
    [HttpPut("user/create", Name = "user_create")]
    [Authorize(Policy = ManagementApiPolicies.Admin)]
    [Consumes("application/json")]
    public IActionResult Put([BindRequired, FromBody]AltSourceNewUserDto input)
    {
      try
      {
        UserInputValidation.ValidateUsername(input.Username);
        UserInputValidation.ValidatePassword(input.Password);
        UserInputValidation.ValidateName(input.FirstName);
        UserInputValidation.ValidateName(input.LastName);
        UserInputValidation.ValidateAddress(input.Address);

        var encodedAddress = HtmlEncoder.Default.Encode(input.Address);

        // ReSharper disable once UnusedVariable
        var result = Users.CreateAltSourceUser(_userManager,
                                               input.Username,
                                               input.Password,
                                               input.FirstName,
                                               input.LastName,
                                               encodedAddress,
                                               false)
                          .Result;

        if (!result.Succeeded)
        {
          return BadRequest(result.CompileErrorMessage());
        }
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }

      return Ok();
    }
  }
}
