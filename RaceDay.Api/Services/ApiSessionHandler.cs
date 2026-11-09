namespace RaceDay.Web.Services;

// Remembers the API's login cookie in this user's MVC session and sends it on every API call
public class ApiSessionHandler : DelegatingHandler
{
    private const string Key = "ApiCookie";
    private readonly IHttpContextAccessor _accessor;

    public ApiSessionHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var session = _accessor.HttpContext?.Session;
        var saved = session?.GetString(Key);
        if (!string.IsNullOrEmpty(saved))
            request.Headers.TryAddWithoutValidation("Cookie", saved);

        var response = await base.SendAsync(request, ct);

        if (session != null && response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var cookie = values.FirstOrDefault(v => v.StartsWith(".AspNetCore.Cookies="));
            if (cookie != null) session.SetString(Key, cookie.Split(';')[0]);
        }
        return response;
    }
}