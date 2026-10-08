using System.Globalization;

namespace UniflowPortal.Services;

public static partial class ApiRoutes
{
    const string V = "/api/v1/";

    // auth
    public const string AdminLogin = V + "auth/admin/login";
    public const string CustomerLogin = V + "auth/customer/login";
    public const string PlatformLogin = V + "platform/login";
    public const string Logout = V + "auth/logout";
    public const string PlatformLogout = V + "platform/logout";
    public const string AdminChangePassword = V + "auth/admin/change-password";

    // admin dashboard
    public const string Dashboard = V + "admin/dashboard-comprehensive";
    public const string DashboardBasic = V + "admin/dashboard";
    public const string RevenueAnalytics = V + "admin/revenue-analytics";
    public const string FinancialSummary = V + "admin/financial-summary";
    public const string OutstandingCustomers = V + "admin/outstanding-customers";
    public const string ActivityLog = V + "admin/activity-log";
    public const string EquityCredentials = V + "admin/equity-credentials";

    // customers
    public const string Customers = V + "customers";
    public static string Customer(int id) => $"{Customers}/{id}";
    public static string AccountSummary(int id) => $"{Customers}/{id}/account-summary";
    public static string ToggleStatus(int id) => $"{Customers}/{id}/toggle-status";
    public static string ResetPassword(int id) => $"{Customers}/{id}/reset-password";
    public static string AdjustBalance(int id) => $"{Customers}/{id}/adjust-balance";
    public const string CustomerStats = V + "customers/stats";
    public const string ZoneAnalytics = V + "customers/analytics/zones";

    // bills
    public const string Bills = V + "bills";
    public const string BillsGenerate = V + "bills/generate";
    public const string BillsMarkOverdue = V + "bills/mark-overdue";
    public const string BillsOverdue = V + "bills/overdue";
    public const string BillsStats = V + "bills/stats";
    public static string BillsForCustomer(int id) => $"{Bills}/customer/{id}";
    public static string BillStatus(int id) => $"{Bills}/{id}/status";
    public static string Bill(int id) => $"{Bills}/{id}";

    // payments / receipts
    public const string PaymentProcess = V + "payments/process";
    public const string PaymentsAll = V + "payments/all";
    public const string PaymentsStats = V + "payments/stats";
    public static string PaymentsForCustomer(int id) => $"{V}payments/customer/{id}";
    public const string Receipts = V + "receipts";
    public static string ReceiptPdf(int id) => $"{Receipts}/{id}/pdf";

    // contributions
    public const string Contributions = V + "contributions";
    public const string ContributionsGenerate = V + "contributions/generate";
    public const string ContributionsSummary = V + "contributions/summary";
    public static string ContributionsForCustomer(int id) => $"{Contributions}/customer/{id}";
    public static string ContributionPaid(int id) => $"{Contributions}/{id}/mark-paid";

    // fines
    public const string Fines = V + "fines";
    public static string FinesForCustomer(int id) => $"{Fines}/customer/{id}";
    public static string FineStatus(int id) => $"{Fines}/{id}/status";

    // sms
    public const string NotifCustomers = V + "notifications/customers";
    public const string NotifHistory = V + "notifications/history";
    public const string BillReminders = V + "admin/notifications/bill-reminders";
    public const string SmsStatus = V + "admin/sms/status";
    public const string SmsSend = V + "admin/sms/send";
    public const string SmsBulk = V + "admin/sms/bulk-send";

    // meter readings
    public const string MeterCustomers = V + "admin/meter-readings/customers";
    public const string MeterBatch = V + "admin/meter-readings";

    // settings
    public static string SettingsConfig(string cat) => $"{V}settings/{cat}/config";
    public const string SettingsInitialize = V + "settings/initialize";

    // Platform schemes (SuperAdmin) — never auto-append schemeId
    public static string Schemes => V + "platform/schemes";
    public static string Scheme(int id) => $"{V}platform/schemes/{id}";
    public static string SchemeToggle(int id) => $"{V}platform/schemes/{id}/toggle-status";
    public static string SchemeAdmins(int id) => $"{V}platform/schemes/{id}/admins";
    public static string SchemeAdminToggle(int schemeId, int adminId)
        => $"{V}platform/schemes/{schemeId}/admins/{adminId}/toggle-status";

    //Scheme-scoped routes get ?schemeId= for SuperAdmin.
    public static bool IsSchemeScoped(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var path = url.Split('?', 2)[0].Trim().ToLowerInvariant();
        if (!path.StartsWith('/'))
            path = "/" + path;

        if (path.StartsWith("/api/v1/platform") || path.StartsWith("/api/v1/auth"))
            return false;

        if (path.StartsWith("/api/v1/"))
            return true;

        return false;
    }

    public static string Q(string url, params (string Key, object? Value)[] query)
    {
        if (query is null || query.Length == 0)
            return url;

        var parts = query
            .Where(p => p.Value is not null && !string.IsNullOrWhiteSpace(Convert.ToString(p.Value, CultureInfo.InvariantCulture)))
            .Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(Convert.ToString(p.Value, CultureInfo.InvariantCulture)!)}");

        var qs = string.Join("&", parts);
        if (string.IsNullOrEmpty(qs))
            return url;

        return url.Contains('?') ? $"{url}&{qs}" : $"{url}?{qs}";
    }
}
