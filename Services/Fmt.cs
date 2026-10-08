using System.Globalization;
using System.Text.RegularExpressions;
using MudBlazor;
namespace UniflowPortal.Services;

public enum ColKind { Text, Mono, Money, Date, DateTime, Month, Status, Balance }
public record Col(string Header, string[] Names, ColKind Kind = ColKind.Text, string[]? SubNames = null, ColKind SubKind = ColKind.Text, string SubPrefix = "");
public record NavItem(string Text, string Href, string Icon, bool Exact = false);

public static class NavItems
{
    public static readonly NavItem[] Admin =
    {
        new("Dashboard", "/", Icons.Material.Filled.Dashboard, true),
        new("Customers", "/customers", Icons.Material.Filled.People),
        new("Billing", "/billing", Icons.Material.Filled.Receipt),
        new("Payments", "/payments", Icons.Material.Filled.CreditCard),
        new("Contributions", "/contributions", Icons.Material.Filled.TrendingUp),
        new("Fines", "/fines", Icons.Material.Filled.Gavel),
        new("Meters", "/meter-readings", Icons.Material.Filled.Speed),
        new("Notifications", "/notifications", Icons.Material.Filled.Notifications),
        new("Settings", "/settings", Icons.Material.Filled.Settings),
    };
    public static readonly NavItem[] Platform = { new("Schemes", "/schemes", Icons.Material.Filled.Hub) };
}

public static class Fmt
{
    public static readonly DialogOptions Dlg = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
    public static readonly DialogOptions DlgLg = new() { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };

    public static string Kes(decimal v) => "KES " + v.ToString("N2", CultureInfo.InvariantCulture);
    public static string Date(string s, string f = "dd/MM/yyyy") =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString(f, CultureInfo.InvariantCulture) : s;
    public static string Iso(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    public static string Pretty(string n) => Regex.Replace(n.Replace("_", " "), "(?<=[a-z])(?=[A-Z])", " ").Trim() is { Length: > 0 } s ? char.ToUpper(s[0]) + s[1..] : n;

    public static string Cell(JsonRow r, ColKind k, string[] n) => k switch
    {
        ColKind.Money => Kes(r.Dec(n)),
        ColKind.Date => Date(r.Str(n)),
        ColKind.DateTime => Date(r.Str(n), "dd/MM/yyyy HH:mm"),
        ColKind.Month => Date(r.Str(n), "MMM yyyy"),
        _ => r.Str(n),
    };

    public static string Status(JsonRow r, string[] n)
    {
        var s = r.Str(n);
        if (s != "") return s;
        return r.Has("isActive") ? (r.Bool("isActive") ? "Active" : "Inactive") : "";
    }

    static readonly Regex NotMoney = new("count|customers|number|days|id$|month|year", RegexOptions.IgnoreCase);
    static readonly Regex IsMoneyRx = new("amount|total|revenue|balance|billed|collected|outstanding|paid|owed|debt|credit|fine|income|arrears", RegexOptions.IgnoreCase);
    public static bool IsMoney(string name) => IsMoneyRx.IsMatch(name) && !NotMoney.IsMatch(name);
}
