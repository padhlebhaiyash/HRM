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

                if (sessionRoles.Contains("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (portal == "Manager")
                    return RedirectToAction("Dashboard", "Manager");
                else
                    return RedirectToAction("Dashboard", "EmployeeSelf");
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
            var roleNames = user.UserRoles?.Select(ur => ur.Role?.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
            var rolesString = string.Join(",", roleNames);

            if (roleMode == "Employee" && roleNames.Contains("Admin"))
            {
                ModelState.AddModelError(string.Empty, "Access denied. Admins must log in through the Manager tab.");
                return View();
            }
            if (roleMode == "Manager" && !roleNames.Contains("Admin") && !roleNames.Contains("Manager") && !roleNames.Contains("HR"))
            {
                ModelState.AddModelError(string.Empty, "Access denied. Only Management or HR can log in through the Manager tab.");
                return View();
            }

            // Set session variables
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRoles", rolesString);
            HttpContext.Session.SetString("LoginPortal", roleMode); // "Employee" or "Manager"
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

            // Route based on portal selected at login
            if (roleNames.Contains("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            else if (roleMode == "Manager")
                return RedirectToAction("Dashboard", "Manager");
            else
                return RedirectToAction("Dashboard", "EmployeeSelf");
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
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();
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
