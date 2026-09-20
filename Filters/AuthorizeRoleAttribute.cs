using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMSystem.Filters
{
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var sessionRoles = session.GetString("UserRoles"); 

            if (string.IsNullOrEmpty(sessionRoles))
            {
                // Not logged in or no roles assigned
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (_roles != null && _roles.Length > 0)
            {
                var assignedRoles = sessionRoles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(r => r.Trim()).ToList();

                // If the user has NONE of the required roles mapped onto the Controller filter
                if (!_roles.Any(requiredRole => assignedRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase)))
                {
                    // Logged in but unauthorized role
                    context.Result = new ForbidResult();
                    return;
                }
            }
            
            base.OnActionExecuting(context);
        }
    }
}
