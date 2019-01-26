using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordMultiRP.Web.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl });
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties {RedirectUri = "/"});
        }

        public IActionResult AccessDenied(string ReturnUrl)
        {
            return View((object)ReturnUrl);
        }
    }
}