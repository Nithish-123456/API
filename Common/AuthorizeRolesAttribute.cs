using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyWebApi.Interfaces;

namespace MyWebApi.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRolesAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _requiredRoles;

    public AuthorizeRolesAttribute(params string[] roles)
    {
        _requiredRoles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        if (!currentUserService.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new ApiResponse<object>
            {
                Success = false,
                Message = "User is not authenticated"
            });
            return;
        }

        if (_requiredRoles.Any() && !HasRequiredRoles(currentUserService.User, _requiredRoles))
        {
            context.Result = new ForbidResult();
            return;
        }
    }

    private bool HasRequiredRoles(Models.User? user, string[] requiredRoles)
    {
        if (user == null || !requiredRoles.Any())
            return false;

        // Implement your role checking logic here
        // This is a simple example - you might want to check against a UserRoles table
        
        foreach (var role in requiredRoles)
        {
            switch (role.ToLower())
            {
                case "admin":
                    if (user.Email.Contains("admin", StringComparison.OrdinalIgnoreCase))
                        return true;
                    break;
                    
                case "manager":
                    if (user.Email.Contains("manager", StringComparison.OrdinalIgnoreCase))
                        return true;
                    break;
                    
                case "user":
                    // All authenticated users have basic user role
                    return true;
            }
        }

        return false;
    }
}
