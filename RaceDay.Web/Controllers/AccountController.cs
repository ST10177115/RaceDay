using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _factory;
    public AccountController(IHttpClientFactory factory) => _factory = factory;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var client = _factory.CreateClient("api");
            var response = await client.PostAsJsonAsync("api/auth/login", new { model.Email, model.Password });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }

            var user = await response.Content.ReadFromJsonAsync<ApiUser>();
            await StartSession(user!);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "RaceDay is unavailable right now. Please try again shortly.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var client = _factory.CreateClient("api");
            var response = await client.PostAsJsonAsync("api/auth/register",
                new { model.FullName, model.Email, model.Password, model.Role, model.OrganiserCode });

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    TempData["Success"] = "Account created. Please log in.";
                    return RedirectToAction(nameof(Login));
                case HttpStatusCode.Forbidden:
                    ModelState.AddModelError("", "That organiser invite code is not valid.");
                    break;
                case HttpStatusCode.Conflict:
                    ModelState.AddModelError("", "An account with that email already exists.");
                    break;
                default:
                    ModelState.AddModelError("", "We could not create your account. Please check your details and try again.");
                    break;
            }
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "RaceDay is unavailable right now. Please try again shortly.");
            return View(model);
        }
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try { await _factory.CreateClient("api").PostAsync("api/auth/logout", null); }
        catch (HttpRequestException) { /* still log out locally */ }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // MVC's own login cookie: remembers who the user is and their role on every page
    private async Task StartSession(ApiUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}