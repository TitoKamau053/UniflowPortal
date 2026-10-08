using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace UniflowPortal.Services;

public class PortalAuthStateProvider(ProtectedSessionStorage storage) : AuthenticationStateProvider
{
    const string Key = "uniflow_token";
    static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    AuthenticationState? _state;
    public string? Token { get; private set; }
    public string SchemeName { get; private set; } = "";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_state is not null) return _state;
        try
        {
            var saved = await storage.GetAsync<string>(Key);
            if (saved.Success && !string.IsNullOrEmpty(saved.Value)) return _state = Build(saved.Value);
        }
        catch { }
        return Anonymous;
    }

    public async Task SignInAsync(string token)
    {
        _state = Build(token);
        await storage.SetAsync(Key, token);
        NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    public async Task SignOutAsync()
    {
        Token = null; _state = null; SchemeName = "";
        try { await storage.DeleteAsync(Key); } catch { }
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    AuthenticationState Build(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        if (jwt.ValidTo < DateTime.UtcNow) return Anonymous;
        Token = token;
        var claims = jwt.Claims.ToList();
        string? Get(params string[] names) => names.Select(n => claims.FirstOrDefault(c => c.Type.Equals(n, StringComparison.OrdinalIgnoreCase))?.Value).FirstOrDefault(v => !string.IsNullOrEmpty(v));
        if (Get("type") is { } type) claims.Add(new Claim(ClaimTypes.Role, type));   // admin | superadmin
        claims.Add(new Claim(ClaimTypes.Name, Get("fullName", "name", "username", "unique_name", "sub") ?? "Administrator"));
        SchemeName = Get("schemeName", "scheme_name", "scheme") ?? "";
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
    }
}
