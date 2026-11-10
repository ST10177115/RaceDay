using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly IHttpClientFactory _factory;
    public EventsController(IHttpClientFactory factory) => _factory = factory;

    public async Task<IActionResult> Details(int id)
    {
        var model = new EventDetailsViewModel();
        try
        {
            var response = await _factory.CreateClient("api").GetAsync($"api/events/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                model.Error = "That event could not be found.";
            else if (!response.IsSuccessStatusCode)
                model.Error = "We could not load this event right now. Please try again shortly.";
            else
                model.Event = await response.Content.ReadFromJsonAsync<EventInfo>();
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or System.Text.Json.JsonException)
        {
            model.Error = "We could not load this event right now. Please try again shortly.";
        }

        return View(model);
    }

    [Authorize(Roles = "Participant"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        try
        {
            var response = await _factory.CreateClient("api")
                .PostAsync($"api/categories/{categoryId}/enrolments", null);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                    TempData["Success"] = "You are enrolled! Good luck on race day.";
                    break;
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Conflict:
                    // These come from our own API rules, for example "This category is full."
                    TempData["Error"] = (await response.Content.ReadAsStringAsync()).Trim('"');
                    break;
                case HttpStatusCode.Unauthorized:
                    TempData["Error"] = "Your session has expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                default:
                    TempData["Error"] = "We could not complete your enrolment. Please try again.";
                    break;
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "RaceDay is unavailable right now. Please try again shortly.";
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}