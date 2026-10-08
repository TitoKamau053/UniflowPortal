using Microsoft.JSInterop;

namespace UniflowPortal.Services;

/// In-memory + sessionStorage scheme context for SuperAdmin overall admin.
public sealed class PortalSession
{
    private const string StorageKey = "uf_active_scheme_id";
    private const string StorageNameKey = "uf_active_scheme_name";
    private const string StorageUserTypeKey = "uf_user_type";

    public string UserType { get; set; } = "";
    public int? TokenSchemeId { get; set; }
    public int? ActiveSchemeId { get; set; }
    public string? ActiveSchemeName { get; set; }

    //True after RestoreAsync / SetUserType has run for this circuit.
    public bool IsReady { get; private set; }

    public bool IsSuperAdmin =>
        UserType.Equals("superadmin", StringComparison.OrdinalIgnoreCase)
        || UserType.Equals("platform", StringComparison.OrdinalIgnoreCase)
        || UserType.Equals("platformadmin", StringComparison.OrdinalIgnoreCase);

    public bool IsSchemeAdmin =>
        UserType.Equals("admin", StringComparison.OrdinalIgnoreCase)
        || UserType.Equals("schemeadmin", StringComparison.OrdinalIgnoreCase)
        || UserType.Equals("scheme-admin", StringComparison.OrdinalIgnoreCase);

    public bool IsCustomer =>
        UserType.Equals("customer", StringComparison.OrdinalIgnoreCase);

    public bool HasActiveScheme => ActiveSchemeId is > 0;

    public bool RequiresSchemeSelection => IsSuperAdmin && !HasActiveScheme;

    public void SetUserTypeFromRole(string? role)
    {
        var r = (role ?? "").Trim().ToLowerInvariant();
        UserType = r switch
        {
            "superadmin" or "platform" or "platformadmin" or "platform-admin" => "superadmin",
            "admin" or "schemeadmin" or "scheme-admin" or "scheme_admin" => "admin",
            "customer" => "customer",
            _ => r
        };
    }

    public async Task SetActiveSchemeAsync(IJSRuntime js, int? schemeId, string? schemeName)
    {
        ActiveSchemeId = schemeId is > 0 ? schemeId : null;
        ActiveSchemeName = string.IsNullOrWhiteSpace(schemeName) ? null : schemeName;

        try
        {
            if (ActiveSchemeId is int id)
            {
                await js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, id.ToString());
                await js.InvokeVoidAsync("sessionStorage.setItem", StorageNameKey, ActiveSchemeName ?? "");
            }
            else
            {
                await js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
                await js.InvokeVoidAsync("sessionStorage.removeItem", StorageNameKey);
            }
        }
        catch { /* SSR */ }
    }

    public async Task PersistUserTypeAsync(IJSRuntime js)
    {
        try
        {
            if (!string.IsNullOrEmpty(UserType))
                await js.InvokeVoidAsync("sessionStorage.setItem", StorageUserTypeKey, UserType);
            else
                await js.InvokeVoidAsync("sessionStorage.removeItem", StorageUserTypeKey);
        }
        catch { }
    }

    public async Task RestoreAsync(IJSRuntime js)
    {
        try
        {
            var idStr = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            var name = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageNameKey);
            var ut = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageUserTypeKey);

            if (int.TryParse(idStr, out var id) && id > 0)
            {
                ActiveSchemeId = id;
                ActiveSchemeName = string.IsNullOrWhiteSpace(name) ? null : name;
            }

            if (!string.IsNullOrWhiteSpace(ut) && string.IsNullOrEmpty(UserType))
                SetUserTypeFromRole(ut);
        }
        catch { }

        IsReady = true;
    }

    public void Clear()
    {
        UserType = "";
        TokenSchemeId = null;
        ActiveSchemeId = null;
        ActiveSchemeName = null;
        IsReady = false;
    }

    public async Task ClearAsync(IJSRuntime js)
    {
        Clear();
        try
        {
            await js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
            await js.InvokeVoidAsync("sessionStorage.removeItem", StorageNameKey);
            await js.InvokeVoidAsync("sessionStorage.removeItem", StorageUserTypeKey);
        }
        catch { }
    }
}
