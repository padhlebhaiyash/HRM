using HRMSystem.Data;
using HRMSystem.Models;
using HRMSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public AuthController(AuthService authService, EmailService emailService, IConfiguration config, ApplicationDbContext context)
        {
            _authService = authService;
            _emailService = emailService;
            _config = config;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            /// Check if user is already logged in
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                var sessionRoles = HttpContext.Session.GetString("UserRoles") ?? "";
                var portal = HttpContext.Session.GetString("LoginPortal") ?? "Employee";

                if (portal == "Employee")
                    return RedirectToAction("Dashboard", "EmployeeSelf");
                else if (sessionRoles.Contains("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else
                    return RedirectToAction("Dashboard", "Manager");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string roleMode = "Employee")
        {
            var user = await _authService.ValidateUserAsync(username, password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            // Enforce role checks logically over the injected DB string tokens
            var roleNames = user.UserRoles?.Select(ur => ur.Role?.Name).Where(n => !string.IsNullOrEmpty(n)).Select(n => n!).ToList() ?? new List<string>();

            // Dynamically detect Manager status by Designation or Reporting Subordinates (avoiding reliance on hardcoded IDs)
            bool isManager = roleNames.Contains("Manager", StringComparer.OrdinalIgnoreCase);
            if (!isManager && user.EmployeeId.HasValue)
            {
                var emp = user.Employee;
                var desigName = emp?.Designation?.Name?.Trim();
                if (!string.IsNullOrEmpty(desigName) && desigName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    isManager = true;
                }
                else
                {
                    // Or if other active employees report to this employee as their Manager
                    isManager = await _context.Employees.AnyAsync(e => e.ManagerId == user.EmployeeId.Value && e.IsActive);
                }

                if (isManager)
                {
                    roleNames.Add("Manager");

                    // Dynamically map role in database by Name so subsequent AuthorizeRole filters pass seamlessly
                    var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
                    if (managerRole != null && !_context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == managerRole.Id))
                    {
                        _context.UserRoles.Add(new UserRoleMapping { UserId = user.Id, RoleId = managerRole.Id });
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Also dynamically detect Admin status by Designation if not already assigned
            bool isAdmin = roleNames.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            if (!isAdmin && user.EmployeeId.HasValue)
            {
                var emp = user.Employee;
                var desigName = emp?.Designation?.Name?.Trim();
                if (!string.IsNullOrEmpty(desigName) && desigName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    roleNames.Add("Admin");
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null && !_context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id))
                    {
                        _context.UserRoles.Add(new UserRoleMapping { UserId = user.Id, RoleId = adminRole.Id });
                        await _context.SaveChangesAsync();
                    }
                }
            }

            var rolesString = string.Join(",", roleNames);

            bool isManagerOrAdmin = roleNames.Contains("Admin", StringComparer.OrdinalIgnoreCase) || 
                                    roleNames.Contains("Manager", StringComparer.OrdinalIgnoreCase) || 
                                    roleNames.Contains("HR", StringComparer.OrdinalIgnoreCase);

            if (roleMode == "Employee" && roleNames.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Access denied. Admins must log in through the Manager tab.");
                return View();
            }
            if (roleMode == "Manager" && !isManagerOrAdmin)
            {
                ModelState.AddModelError(string.Empty, "Access denied. Only Management or HR can log in through the Manager tab.");
                return View();
            }

            // Determine active login portal:
            string activePortal = roleMode;

            // Set session variables
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRoles", rolesString);
            HttpContext.Session.SetString("LoginPortal", activePortal); // "Employee" or "Manager"
            HttpContext.Session.SetString("Username", user.Username);
            
            if (user.EmployeeId.HasValue)
            {
                HttpContext.Session.SetString("EmployeeId", user.EmployeeId.Value.ToString());
                HttpContext.Session.SetString("UserFullName", user.Employee?.FullName ?? user.Username);
            }
            else
            {
                HttpContext.Session.SetString("UserFullName", "System Admin");
            }

            if (user.MustChangePassword)
            {
                return RedirectToAction("ResetPassword");
            }

            // Route based on portal selected at login:
            // Whenever an employee logs in, they always land on the Employee Dashboard page
            if (roleMode == "Employee")
                return RedirectToAction("Dashboard", "EmployeeSelf");
            else if (roleNames.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Dashboard", "Manager");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login");
                
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation do not match.");
                return View();
            }

            int userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            bool success = await _authService.ProcessFirstLoginResetAsync(userId, oldPassword, newPassword);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to reset password. Check your old password.");
                return View();
            }

            TempData["SuccessMessage"] = "Password reset successfully. Please login with your new password.";
            return RedirectToAction("Logout");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Please enter your registered email address.");
                return View();
            }

            var results = await _authService.GenerateAllPasswordResetTokensAsync(email);

            if (results != null && results.Any())
            {
                int expiryMinutes = _config.GetValue<int>("EmailSettings:TokenExpiryMinutes", 30);
                var firstUser = results.First().user;
                var toName = firstUser.Employee?.FullName ?? firstUser.Username;

                try
                {
                    if (results.Count == 1)
                    {
                        var res = results.First();
                        var resetLink = await BuildResetPasswordLinkAsync(res.token);
                        await _emailService.SendPasswordResetEmailAsync(
                            email, 
                            toName, 
                            resetLink, 
                            expiryMinutes);
                    }
                    else
                    {
                        var accounts = new List<(string username, string resetLink)>();
                        foreach (var res in results)
                        {
                            var resetLink = await BuildResetPasswordLinkAsync(res.token);
                            accounts.Add((res.user.Username, resetLink));
                        }
                        await _emailService.SendMultiplePasswordResetEmailAsync(
                            email, 
                            toName, 
                            accounts, 
                            expiryMinutes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to send password reset email to {email}: {ex.Message}");
                    TempData["EmailWarning"] = "There was an issue delivering the notification email. Please check your SMTP settings or contact support.";
                }
            }

            TempData["ResetEmail"] = email;
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            if (TempData["ResetEmail"] == null)
                return RedirectToAction("ForgotPassword");

            ViewBag.Email = TempData["ResetEmail"];
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPasswordViaToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");

            var user = await _authService.ValidateResetTokenAsync(token);
            if (user == null)
            {
                return View("ResetPasswordExpired");
            }

            ViewBag.Token = token;
            ViewBag.Username = user.Username;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordViaToken(string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation do not match.");
                ViewBag.Token = token;
                return View();
            }

            bool success = await _authService.CompletePasswordResetAsync(token, newPassword);
            if (!success)
            {
                // Token might have expired while they were filling out the form
                return View("ResetPasswordExpired");
            }

            TempData["SuccessMessage"] = "Your password has been successfully reset. Please log in with your new password.";
            return RedirectToAction("Login");
        }

        private async Task<string> BuildResetPasswordLinkAsync(string token)
        {
            var companySetting = await _context.CompanySettings.OrderBy(c => c.Id).FirstOrDefaultAsync();
            var baseUrl = companySetting?.AppBaseUrl?.TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return $"{baseUrl}/Auth/ResetPasswordViaToken?token={System.Net.WebUtility.UrlEncode(token)}";
            }

            return Url.Action("ResetPasswordViaToken", "Auth", new { token }, Request.Scheme) 
                   ?? $"{Request.Scheme}://{Request.Host}/Auth/ResetPasswordViaToken?token={System.Net.WebUtility.UrlEncode(token)}";
        }
    }
}
