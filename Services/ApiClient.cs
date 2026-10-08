using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UniflowPortal.Services;

public class ApiClient(IHttpClientFactory factory, PortalAuthStateProvider auth, PortalSession session)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    HttpClient C()
    {
        var c = factory.CreateClient("api");
        if (!string.IsNullOrEmpty(auth.Token))
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return c;
    }

    private string ApplySchemeContext(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // Already has schemeId
        if (url.Contains("schemeId=", StringComparison.OrdinalIgnoreCase))
            return url;

        // Need a selected scheme id
        if (session.ActiveSchemeId is not int sid || sid <= 0)
            return url;

        // Only SuperAdmin should send schemeId (scheme admin uses JWT scheme)
        // If UserType not set yet but ActiveSchemeId is set, still append — that only happens for SuperAdmin flows.
        if (!session.IsSuperAdmin && !string.IsNullOrEmpty(session.UserType))
            return url;

        if (!ApiRoutes.IsSchemeScoped(url))
            return url;

        var sep = url.Contains('?') ? "&" : "?";
        return $"{url}{sep}schemeId={sid}";
    }

    public Task<ApiResponse> Get(string url)
        => Send(c => c.GetAsync(ApplySchemeContext(url)));

    public Task<ApiResponse> Post(string url, object? body = null)
        => Send(c => c.PostAsJsonAsync(ApplySchemeContext(url), body ?? new { }, Json));

    public Task<ApiResponse> Put(string url, object? body)
        => Send(c => c.PutAsJsonAsync(ApplySchemeContext(url), body, Json));

    public Task<ApiResponse> Delete(string url)
        => Send(c => c.DeleteAsync(ApplySchemeContext(url)));

    public async Task<byte[]?> Bytes(string url)
    {
        try
        {
            var r = await C().GetAsync(ApplySchemeContext(url));
            return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
        }
        catch { return null; }
    }

    public Task<ApiResponse> SuperAdminLoginAsync(string username, string password)
        => LoginAsync(ApiRoutes.PlatformLogin, new { username, password });

    public Task<ApiResponse> AdminLoginAsync(string username, string password)
        => LoginAsync(ApiRoutes.AdminLogin, new { username, password });

    public Task<ApiResponse> CustomerLoginAsync(string accountNo, string password)
        => LoginAsync(ApiRoutes.CustomerLogin, new { accountNo, password });

    private async Task<ApiResponse> LoginAsync(string endpoint, object payload)
    {
        try
        {
            var client = factory.CreateClient("api");
            using var res = await client.PostAsJsonAsync(endpoint, payload, Json);
            var raw = await res.Content.ReadAsStringAsync();
            var result = ApiResponse.Parse(raw, (int)res.StatusCode, res.IsSuccessStatusCode);

            if (!result.Success)
                return result;

            var token = new JsonRow(result.Root)
                .Str("token", "accessToken", "jwt");

            if (string.IsNullOrWhiteSpace(token) &&
                res.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                token = cookies
                    .Select(c => c.Split(';')[0])
                    .FirstOrDefault(c => c.StartsWith("water_token="))
                    ?["water_token=".Length..] ?? "";
            }

            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse.Fail("Signed in, but the API returned no authentication token.");

            await auth.SignInAsync(token);
            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail(ex.Message);
        }
    }

    public async Task LogoutAsync(bool platform)
    {
        try { await Post(platform ? ApiRoutes.PlatformLogout : ApiRoutes.Logout); } catch { }
        session.Clear();
        await auth.SignOutAsync();
    }

    async Task<ApiResponse> Send(Func<HttpClient, Task<HttpResponseMessage>> call)
    {
        try
        {
            using var res = await call(C());
            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await auth.SignOutAsync();
                return ApiResponse.Fail("Session expired, please sign in again.", 401);
            }

            var parsed = ApiResponse.Parse(
                await res.Content.ReadAsStringAsync(),
                (int)res.StatusCode,
                res.IsSuccessStatusCode);

            if (!parsed.Success &&
                !string.IsNullOrEmpty(parsed.Message) &&
                parsed.Message.Contains("schemeId is required", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse.Fail(
                    "Select a scheme from the header first. " + parsed.Message,
                    parsed.Status);
            }

            return parsed;
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail(ex.Message);
        }
    }
}
