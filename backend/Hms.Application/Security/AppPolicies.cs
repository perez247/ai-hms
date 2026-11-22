namespace Hms.Application.Security;

/// <summary>Role-only policies for features that are intentionally restricted to one staff role.</summary>
public static class AppPolicies
{
    public const string NurseOnly = "roles.nurse";
}
