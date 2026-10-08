using System.Security.Claims;
using MudBlazor;
using UniflowPortal.Services;

public sealed class MenuItem
{
    public string Text { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string? Href { get; init; }
    public bool Exact { get; init; }
    public bool IsVisibleToAll { get; init; }
    public IReadOnlyList<string> AllowedRoles { get; init; } = Array.Empty<string>();
    public List<MenuItem> Children { get; init; } = new();
    public bool HasChildren => Children.Count > 0;

    public bool IsAllowed(ClaimsPrincipal user)
    {
        if (IsVisibleToAll) return true;
        if (AllowedRoles.Count == 0) return true;
        return AllowedRoles.Any(r => user.IsInRole(r) || user.HasClaim("role", r) || user.HasClaim(ClaimTypes.Role, r));
    }

    public IEnumerable<MenuItem> VisibleChildren(ClaimsPrincipal user)
        => Children.Where(child => child.IsAllowed(user) && child.HasVisibleBranch(user));

    public bool HasVisibleBranch(ClaimsPrincipal user)
        => IsAllowed(user) && (!HasChildren || Children.Any(child => child.IsAllowed(user) && child.HasVisibleBranch(user)));
}

public static class NavItems
{
    public static List<MenuItem> ForUser(ClaimsPrincipal user, PortalSession? session = null)
    {
        bool HasRole(string role) =>
            user.IsInRole(role) || user.HasClaim("role", role) || user.HasClaim(ClaimTypes.Role, role);

        if (HasRole("superadmin"))
            return PlatformAdmin(session);

        if (HasRole("admin"))
            return SchemeAdmin();

        if (HasRole("customer"))
            return Customer();

        return new();
    }

    /// <summary>
    /// SuperAdmin: always Dashboard + Schemes.
    /// When ActiveSchemeId is set, also expose full scheme-ops menu.
    /// </summary>
    public static List<MenuItem> PlatformAdmin(PortalSession? session = null)
    {
        var items = new List<MenuItem>
        {
            new() { Text = "Dashboard", Icon = Icons.Material.Filled.Dashboard, Href = "/", Exact = true },
            new() { Text = "Schemes", Icon = Icons.Material.Filled.WaterDrop, Href = "/schemes" },
        };

        if (session?.HasActiveScheme == true)
            items.AddRange(SchemeOpsItems());

        return items;
    }

    public static List<MenuItem> SchemeAdmin() =>
        new List<MenuItem>
        {
            new() { Text = "Dashboard", Icon = Icons.Material.Filled.Dashboard, Href = "/", Exact = true },
        }.Concat(SchemeOpsItems()).ToList();

    private static List<MenuItem> SchemeOpsItems() => new()
    {
        new() { Text = "Customers", Icon = Icons.Material.Filled.People, Href = "/customers" },
        new()
        {
            Text = "Billing",
            Icon = Icons.Material.Filled.Receipt,
            Children = new()
            {
                new() { Text = "Bills", Href = "/billing" },
                new() { Text = "Contributions", Href = "/contributions" },
                new() { Text = "Fines", Href = "/fines" }
            }
        },
        new() { Text = "Payments", Icon = Icons.Material.Filled.Payments, Href = "/payments" },
        new()
        {
            Text = "Operations",
            Icon = Icons.Material.Filled.Build,
            Children = new()
            {
                new() { Text = "Meter Readings", Href = "/meter-readings" },
                new() { Text = "Notifications", Href = "/notifications" }
            }
        },
        new() { Text = "Settings", Icon = Icons.Material.Filled.Settings, Href = "/settings" }
    };

    public static List<MenuItem> Customer() => new()
    {
        new() { Text = "Dashboard", Icon = Icons.Material.Filled.Dashboard, Href = "/", Exact = true }
    };
}
