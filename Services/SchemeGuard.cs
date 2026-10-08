namespace UniflowPortal.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

public static class SchemeGuard
{
    public static bool EnsureSchemeOrRedirect(
        PortalSession session,
        NavigationManager nav,
        ISnackbar snack)
    {
        if (!session.IsSuperAdmin)
            return true;
        if (session.HasActiveScheme)
            return true;
        
        snack.Add("Select a scheme first", Severity.Warning);
        nav.NavigateTo("/schemes");
        return false;
    }
}