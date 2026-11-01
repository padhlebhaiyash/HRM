using System.Net;
using System.Net.Mail;
using HRMSystem.Data;
using HRMSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext? _context;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, ApplicationDbContext? context = null)
        {
            _config = config;
            _logger = logger;
            _context = context;
        }

        private (string fromEmail, string fromName, string username, string password, string smtpHost, int smtpPort, bool enableSsl) GetSmtpConfig()
        {
            var settings = _config.GetSection("EmailSettings");
            var smtpHost = (settings["SmtpHost"] ?? "smtp.gmail.com").Trim();
            var smtpPort = int.TryParse(settings["SmtpPort"], out int port) ? port : 587;
            var enableSsl = bool.TryParse(settings["EnableSsl"], out bool ssl) ? ssl : true;
            var defaultFromEmail = (settings["FromEmail"] ?? "techinovexasolutions@gmail.com").Trim();
            var defaultFromName = (settings["FromName"] ?? "MunroHR").Trim();
            var username = (settings["Username"] ?? defaultFromEmail).Trim();
            var password = (settings["Password"] ?? "").Trim();

            // Self-healing fallback if Azure environment variable was not configured or has old/placeholder value
            if (string.IsNullOrWhiteSpace(username) || username.Contains("<"))
            {
                username = "techinovexasolutions@gmail.com";
            }

            if (string.IsNullOrWhiteSpace(password) || password.Contains("<") || password == "ofax jmmq mzuc ioai")
            {
                password = "btqw movk bgdg dtob";
            }

            string fromEmail = defaultFromEmail;
            string fromName = defaultFromName;

            try
            {
                var companySetting = _context?.CompanySettings.OrderBy(c => c.Id).FirstOrDefault();
                if (companySetting != null)
                {
                    if (!string.IsNullOrWhiteSpace(companySetting.FromEmail))
                        fromEmail = companySetting.FromEmail.Trim();
                    if (!string.IsNullOrWhiteSpace(companySetting.FromName))
                        fromName = companySetting.FromName.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load CompanySettings from database, falling back to appsettings.");
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = username;
            }

            return (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl);
        }

        private SmtpClient CreateSmtpClient(string smtpHost, int smtpPort, bool enableSsl, string username, string password)
        {
            return new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 25000
            };
        }

        public static string FormatExpiryDuration(int expiryMinutes)
        {
            if (expiryMinutes >= 1440)
            {
                double days = (double)expiryMinutes / 1440.0;
                if (days % 1 == 0)
                {
                    int d = (int)days;
                    return d == 1 ? "1 day" : $"{d} days";
                }
                return $"{days:0.#} days";
            }
            else if (expiryMinutes >= 60)
            {
                double hours = (double)expiryMinutes / 60.0;
                if (hours % 1 == 0)
                {
                    int h = (int)hours;
                    return h == 1 ? "1 hour" : $"{h} hours";
                }
                return $"{hours:0.#} hours";
            }
            else
            {
                return expiryMinutes == 1 ? "1 minute" : $"{expiryMinutes} minutes";
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, int expiryMinutes)
        {
            var (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();
            string formattedExpiry = FormatExpiryDuration(expiryMinutes);

            string subject = $"{fromName} – Password Reset Request";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{fromName} – Password Reset Request</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f4f4; padding: 20px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dee2e6;"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #0d6efd; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 {fromName}</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 40px; color: #333333;"">
              <h2 style=""color: #0d6efd; margin-top: 0; margin-bottom: 20px; font-size: 20px;"">Password Reset Request</h2>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 24px 0;"">We received a request to reset your {fromName} account password. Click the button below to set a new password:</p>
              
              <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin: 24px 0;"">
                <tr>
                  <td align=""center"">
                    <a href=""{resetLink}"" target=""_blank"" style=""display: inline-block; padding: 14px 36px; background-color: #0d6efd; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;"">Reset My Password</a>
                  </td>
                </tr>
              </table>

              <table border=""0"" cellpadding=""12"" cellspacing=""0"" width=""100%"" style=""background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 8px; margin: 20px 0;"">
                <tr>
                  <td style=""font-size: 14px; color: #856404; line-height: 1.5;"">
                    ⏰ <strong>This link will expire in {formattedExpiry}.</strong> If you do not reset your password within this time, you will need to make a new request.
                  </td>
                </tr>
              </table>

              <p style=""font-size: 14px; line-height: 1.5; margin: 24px 0 8px 0; color: #6c757d;"">If the button above doesn't work, copy and paste this URL into your browser:</p>
              <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 12px 16px; word-break: break-all; font-size: 13px; color: #495057; font-family: monospace;"">{resetLink}</div>
              
              <p style=""font-size: 13px; line-height: 1.5; margin: 24px 0 0 0; color: #6c757d;"">
                If you did not request a password reset, please ignore this email. Your password will remain unchanged.
              </p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #f8f9fa; padding: 20px 40px; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6;"">
              &copy; {DateTime.Now.Year} {fromName}. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = CreateSmtpClient(smtpHost, smtpPort, enableSsl, username, password);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail, toName));

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendMultiplePasswordResetEmailAsync(string toEmail, string toName, List<(string username, string resetLink)> accounts, int expiryMinutes)
        {
            var (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();
            string formattedExpiry = FormatExpiryDuration(expiryMinutes);

            string subject = $"{fromName} – Password Reset Requests";
            
            var accountsHtml = new System.Text.StringBuilder();
            foreach (var acc in accounts)
            {
                accountsHtml.AppendLine($@"
                    <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 16px; margin-bottom: 16px;"">
                        <p style=""font-size: 16px; margin: 0 0 10px 0; color: #495057;"">
                            <strong>Username:</strong> <span style=""font-family: monospace; font-size: 17px; color: #0d6efd;"">{acc.username}</span>
                        </p>
                        <a href=""{acc.resetLink}"" target=""_blank"" style=""display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px;"">Reset Password for {acc.username}</a>
                    </div>");
            }

            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{fromName} – Password Reset Requests</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f4f4; padding: 20px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dee2e6;"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #0d6efd; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 {fromName}</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 40px; color: #333333;"">
              <h2 style=""color: #0d6efd; margin-top: 0; margin-bottom: 20px; font-size: 20px;"">Multiple Accounts Found</h2>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 24px 0;"">We found multiple active user accounts associated with your email address. Please choose the account you wish to reset below:</p>
              
              {accountsHtml.ToString()}

              <table border=""0"" cellpadding=""12"" cellspacing=""0"" width=""100%"" style=""background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 8px; margin: 20px 0;"">
                <tr>
                  <td style=""font-size: 14px; color: #856404; line-height: 1.5;"">
                    ⏰ <strong>These links will expire in {formattedExpiry}.</strong> If you do not reset your password within this time, you will need to make a new request.
                  </td>
                </tr>
              </table>

              <p style=""font-size: 14px; color: #6c757d; line-height: 1.5; margin: 24px 0 0 0;"">
                If you did not request a password reset, please ignore this email.
              </p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #f8f9fa; padding: 20px 40px; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6;"">
              &copy; {DateTime.Now.Year} {fromName}. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = CreateSmtpClient(smtpHost, smtpPort, enableSsl, username, password);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail, toName));

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Password reset links email sent to {Email} containing {Count} account(s)", toEmail, accounts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multiple password reset email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendGenericEmailAsync(string toEmail, string subject, string body)
        {
            var (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();

            using var smtp = CreateSmtpClient(smtpHost, smtpPort, enableSsl, username, password);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(toEmail);

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Generic email '{Subject}' sent to {Email}", subject, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send generic email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendWelcomeAndSetupEmailAsync(string toEmail, string toName, string username, string setupLink, int expiryMinutes)
        {
            var (fromEmail, fromName, smtpUsername, smtpPassword, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();
            string formattedExpiry = FormatExpiryDuration(expiryMinutes);

            string subject = "Welcome to the Team! Set Up Your Account";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Welcome to the Team! Set Up Your Account</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f4f4; padding: 20px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dee2e6;"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #1e3c72; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 {fromName}</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 40px; color: #333333;"">
              <h2 style=""color: #1e3c72; margin-top: 0; margin-bottom: 20px; font-size: 20px;"">Welcome to the Team!</h2>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">Your employee profile has been registered in {fromName}. To get started, please set up your password and complete your profile.</p>

              <table border=""0"" cellpadding=""16"" cellspacing=""0"" width=""100%"" style=""background-color: #e7f1ff; border: 1px solid #b6d4fe; border-radius: 8px; margin: 20px 0;"">
                <tr>
                  <td style=""font-size: 15px; color: #084298; line-height: 1.5;"">
                    <strong>Your Username:</strong> <code style=""background-color: rgba(0,0,0,0.05); padding: 2px 6px; border-radius: 4px; font-family: monospace; font-size: 14px;"">{username}</code>
                  </td>
                </tr>
              </table>

              <p style=""font-size: 16px; line-height: 1.5; margin: 20px 0 24px 0;"">Click the button below to choose your password. After resetting your password, you will be prompted to log in and update your personal details (Father's Name, Date of Birth, Gender, Phone, and Addresses).</p>

              <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin: 24px 0;"">
                <tr>
                  <td align=""center"">
                    <a href=""{setupLink}"" target=""_blank"" style=""display: inline-block; padding: 14px 36px; background-color: #1e3c72; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;"">Set Up My Account</a>
                  </td>
                </tr>
              </table>

              <table border=""0"" cellpadding=""12"" cellspacing=""0"" width=""100%"" style=""background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 8px; margin: 20px 0;"">
                <tr>
                  <td style=""font-size: 14px; color: #856404; line-height: 1.5;"">
                    ⏰ <strong>This setup link will expire in {formattedExpiry}.</strong>
                  </td>
                </tr>
              </table>

              <p style=""font-size: 14px; line-height: 1.5; margin: 24px 0 8px 0; color: #6c757d;"">If the button above doesn't work, copy and paste this URL into your browser:</p>
              <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 12px 16px; word-break: break-all; font-size: 13px; color: #495057; font-family: monospace;"">{setupLink}</div>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #f8f9fa; padding: 20px 40px; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6;"">
              &copy; {DateTime.Now.Year} {fromName}. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = CreateSmtpClient(smtpHost, smtpPort, enableSsl, smtpUsername, smtpPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail, toName));

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Welcome and setup email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome and setup email to {Email}", toEmail);
                throw;
            }
        }

        /// <summary>
        /// Sends leave application notifications to all relevant reporting managers and administrators.
        /// </summary>
        public async Task NotifyLeaveApplicationAsync(int leaveRequestId, string? requestScheme = null, string? requestHost = null)
        {
            if (_context == null)
            {
                _logger.LogWarning("EmailService: Cannot send leave notification because ApplicationDbContext is null.");
                return;
            }

            try
            {
                var leave = await _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e!.Department)
                    .Include(l => l.Employee)
                        .ThenInclude(e => e!.Designation)
                    .Include(l => l.Employee)
                        .ThenInclude(e => e!.Manager)
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == leaveRequestId);

                if (leave == null || leave.Employee == null)
                {
                    _logger.LogWarning("EmailService: Leave request {LeaveRequestId} or associated employee was not found.", leaveRequestId);
                    return;
                }

                var employee = leave.Employee;

                // Build dictionary of unique recipients: Email -> (Name, RoleDescription)
                var recipients = new Dictionary<string, (string Name, string RoleDescription)>(StringComparer.OrdinalIgnoreCase);

                // 1. Collect Reporting Managers (direct manager and up the hierarchy)
                var currentManager = employee.Manager;
                var visitedManagerIds = new HashSet<int> { employee.Id };

                while (currentManager != null && !visitedManagerIds.Contains(currentManager.Id))
                {
                    visitedManagerIds.Add(currentManager.Id);

                    if (currentManager.IsActive && !string.IsNullOrWhiteSpace(currentManager.Email))
                    {
                        var cleanEmail = currentManager.Email.Trim();
                        if (!recipients.ContainsKey(cleanEmail))
                        {
                            recipients[cleanEmail] = (currentManager.FullName, "Reporting Manager");
                        }
                    }

                    if (currentManager.ManagerId.HasValue && currentManager.ManagerId.Value != currentManager.Id)
                    {
                        currentManager = await _context.Employees.FirstOrDefaultAsync(e => e.Id == currentManager.ManagerId.Value);
                    }
                    else
                    {
                        break;
                    }
                }

                // 2. Collect Admins (all active users with Admin or HR role)
                var adminUsers = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.UserRoles!)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.IsActive && u.UserRoles!.Any(ur => ur.Role != null && (ur.Role.Name == "Admin" || ur.Role.Name == "HR")))
                    .ToListAsync();

                bool adminEmailFound = false;

                foreach (var adminUser in adminUsers)
                {
                    string? adminEmail = null;
                    string adminName = "Administrator";

                    if (adminUser.Employee != null && !string.IsNullOrWhiteSpace(adminUser.Employee.Email) && adminUser.Employee.IsActive)
                    {
                        adminEmail = adminUser.Employee.Email.Trim();
                        adminName = adminUser.Employee.FullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(adminUser.Username) && adminUser.Username.Contains("@"))
                    {
                        adminEmail = adminUser.Username.Trim();
                        adminName = adminUser.Username.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(adminEmail))
                    {
                        adminEmailFound = true;
                        if (recipients.TryGetValue(adminEmail, out var existing))
                        {
                            recipients[adminEmail] = (existing.Name, "Reporting Manager & Administrator");
                        }
                        else
                        {
                            recipients[adminEmail] = (adminName, "Administrator");
                        }
                    }
                }

                // Fallback admin email if no active admin with email was found in the database
                if (!adminEmailFound)
                {
                    var companySetting = await _context.CompanySettings.OrderBy(c => c.Id).FirstOrDefaultAsync();
                    var fallbackAdminEmail = _config.GetValue<string>("EmailSettings:AdminEmail")
                        ?? companySetting?.FromEmail
                        ?? "admin@hrmsystem.com";

                    if (!string.IsNullOrWhiteSpace(fallbackAdminEmail))
                    {
                        var cleanFallback = fallbackAdminEmail.Trim();
                        if (!recipients.ContainsKey(cleanFallback))
                        {
                            recipients[cleanFallback] = ("Administrator / HR", "Administrator");
                        }
                    }
                }

                // Never send approval notification to the applying employee themselves
                if (!string.IsNullOrWhiteSpace(employee.Email))
                {
                    recipients.Remove(employee.Email.Trim());
                }

                if (recipients.Count == 0)
                {
                    _logger.LogWarning("EmailService: No valid recipients found for leave request {LeaveRequestId}.", leaveRequestId);
                    return;
                }

                // Determine review URL
                var compSetting = await _context.CompanySettings.OrderBy(c => c.Id).FirstOrDefaultAsync();
                string reviewUrl;
                if (!string.IsNullOrWhiteSpace(compSetting?.AppBaseUrl))
                {
                    reviewUrl = $"{compSetting.AppBaseUrl.TrimEnd('/')}/Leave/Index";
                }
                else if (!string.IsNullOrWhiteSpace(requestScheme) && !string.IsNullOrWhiteSpace(requestHost))
                {
                    reviewUrl = $"{requestScheme}://{requestHost}/Leave/Index";
                }
                else
                {
                    reviewUrl = "/Leave/Index";
                }

                string departmentName = employee.Department?.Name ?? "General";
                string designationName = employee.Designation?.Name ?? "Employee";
                string leaveTypeName = leave.LeaveType?.Name ?? "Leave";
                string datesDisplay = leave.DisplayDates;

                string totalDaysDisplay;
                if (leave.HalfDayPeriod != HalfDayPeriod.None)
                {
                    var halfStr = leave.HalfDayPeriod == HalfDayPeriod.FirstHalf ? "First Half" : "Second Half";
                    totalDaysDisplay = $"0.5 Day ({halfStr})";
                }
                else
                {
                    decimal days = leave.TotalDays;
                    totalDaysDisplay = days == 1 ? "1 Day" : $"{days:0.#} Days";
                }

                string reason = string.IsNullOrWhiteSpace(leave.Reason) ? "No reason specified" : leave.Reason.Trim();

                // Send email to each recipient
                foreach (var recipient in recipients)
                {
                    try
                    {
                        await SendLeaveApplicationEmailAsync(
                            toEmail: recipient.Key,
                            toName: recipient.Value.Name,
                            recipientRole: recipient.Value.RoleDescription,
                            employeeName: employee.FullName,
                            employeeCode: employee.EmployeeCode,
                            departmentName: departmentName,
                            designationName: designationName,
                            leaveTypeName: leaveTypeName,
                            datesDisplay: datesDisplay,
                            totalDaysDisplay: totalDaysDisplay,
                            reason: reason,
                            appliedAt: leave.AppliedAt,
                            reviewUrl: reviewUrl);

                        _logger.LogInformation("EmailService: Leave application email sent to {Email} ({Role}) for employee {EmployeeName}", 
                            recipient.Key, recipient.Value.RoleDescription, employee.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "EmailService: Failed sending leave email to {Email} ({Role}) for employee {EmployeeName}", 
                            recipient.Key, recipient.Value.RoleDescription, employee.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: Unexpected error while notifying managers and admins for leave request {LeaveRequestId}", leaveRequestId);
            }
        }

        public async Task SendLeaveApplicationEmailAsync(
            string toEmail,
            string toName,
            string recipientRole,
            string employeeName,
            string employeeCode,
            string departmentName,
            string designationName,
            string leaveTypeName,
            string datesDisplay,
            string totalDaysDisplay,
            string reason,
            DateTime appliedAt,
            string reviewUrl)
        {
            var (fromEmail, fromName, smtpUsername, smtpPassword, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();

            string subject = $"Leave Application: {employeeName} ({datesDisplay})";

            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>New Leave Request - {employeeName}</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f6f9; padding: 25px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e1e4e8; box-shadow: 0 4px 12px rgba(0,0,0,0.05);"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); background-color: #1e3c72; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;"">🏢 {fromName}</h1>
              <p style=""color: #dbe4f0; margin: 8px 0 0 0; font-size: 14px;"">Human Resource Management System</p>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 35px 40px; color: #333333;"">
              <div style=""display: inline-block; padding: 5px 14px; background-color: #fff3cd; color: #856404; border: 1px solid #ffeeba; border-radius: 16px; font-weight: bold; font-size: 12px; text-transform: uppercase; margin-bottom: 16px;"">
                ⏳ Action Required &bull; Pending Review
              </div>
              <h2 style=""color: #1e3c72; margin-top: 0; margin-bottom: 16px; font-size: 20px;"">New Leave Application</h2>
              <p style=""font-size: 15px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 15px; line-height: 1.5; margin: 0 0 24px 0; color: #555555;"">
                <strong>{employeeName}</strong> has submitted a new leave application awaiting your approval as <strong>{recipientRole}</strong>.
              </p>

              <!-- Leave Details Box -->
              <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; margin-bottom: 24px; overflow: hidden;"">
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; width: 35%; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Employee</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #212529; font-weight: 600;"">{employeeName} <span style=""color: #6c757d; font-weight: normal;"">({employeeCode})</span></td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Department / Role</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #212529;"">{departmentName} &bull; {designationName}</td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Leave Type</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #0d6efd; font-weight: 600;"">{leaveTypeName}</td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Period</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #212529; font-weight: 600;"">{datesDisplay}</td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Total Duration</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #198754; font-weight: 600;"">{totalDaysDisplay}</td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase; vertical-align: top;"">Reason</td>
                  <td style=""padding: 12px 18px; border-bottom: 1px solid #dee2e6; font-size: 14px; color: #333333; line-height: 1.4;"">{reason}</td>
                </tr>
                <tr>
                  <td style=""padding: 12px 18px; color: #6c757d; font-size: 13px; font-weight: bold; text-transform: uppercase;"">Applied At</td>
                  <td style=""padding: 12px 18px; font-size: 13px; color: #6c757d;"">{appliedAt:dd MMM yyyy, hh:mm tt}</td>
                </tr>
              </table>

              <!-- Call to Action Button -->
              <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin: 28px 0 20px 0;"">
                <tr>
                  <td align=""center"">
                    <a href=""{reviewUrl}"" target=""_blank"" style=""display: inline-block; padding: 14px 38px; background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); background-color: #1e3c72; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 15px; letter-spacing: 0.3px; box-shadow: 0 4px 8px rgba(30,60,114,0.25);"">
                      Review & Approve / Reject
                    </a>
                  </td>
                </tr>
              </table>

              <p style=""font-size: 13px; color: #6c757d; line-height: 1.5; margin: 24px 0 6px 0;"">
                If the button above does not work, please access the leave portal directly with this link:
              </p>
              <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 10px 14px; word-break: break-all; font-size: 12px; color: #495057; font-family: monospace;"">{reviewUrl}</div>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #f8f9fa; padding: 20px 40px; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6;"">
              &copy; {DateTime.Now.Year} {fromName}. This is an automated notification sent to {recipientRole}.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = CreateSmtpClient(smtpHost, smtpPort, enableSsl, smtpUsername, smtpPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await smtp.SendMailAsync(message);
        }
    }
}

