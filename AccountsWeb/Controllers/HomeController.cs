using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AccountsWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace AccountsWeb.Controllers
{
  public class HomeController : Controller
  {
    [Authorize]
    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Privacy()
    {
      return View();
    }

    public IActionResult Logout()
    {
      return SignOut("Cookies", "oidc");
    }

    [Authorize]
    public async Task<IActionResult> CallApi()
    {
      var accessToken = await HttpContext.GetTokenAsync("access_token");
      var client = new HttpClient();

      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

      var content = await client.GetStringAsync("http://localhost:5001/identity");

      ViewBag.Json = JArray.Parse(content).ToString();
      return View("Json");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
