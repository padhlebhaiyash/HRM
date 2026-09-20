using System.Net;
using System.Net.Mail;
using HRMSystem.Data;

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
            var smtpHost = settings["SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(settings["SmtpPort"], out int port) ? port : 587;
            var enableSsl = bool.TryParse(settings["EnableSsl"], out bool ssl) ? ssl : true;
            var defaultFromEmail = settings["FromEmail"] ?? "techinovexasolutions@gmail.com";
            var defaultFromName = settings["FromName"] ?? "HRM System";
            var username = settings["Username"] ?? defaultFromEmail;
            var password = settings["Password"] ?? "";

            string fromEmail = defaultFromEmail;
            string fromName = defaultFromName;

            try
            {
                var companySetting = _context?.CompanySettings.FirstOrDefault();
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

            return (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, int expiryMinutes)
        {
            var (fromEmail, fromName, username, password, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();

            string subject = "HRM System – Password Reset Request";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>HRM System – Password Reset Request</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f4f4; padding: 20px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dee2e6;"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #0d6efd; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 HRM System</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 40px; color: #333333;"">
              <h2 style=""color: #0d6efd; margin-top: 0; margin-bottom: 20px; font-size: 20px;"">Password Reset Request</h2>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 24px 0;"">We received a request to reset your HRM System account password. Click the button below to set a new password:</p>
              
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
                    ⏰ <strong>This link will expire in {expiryMinutes} minutes.</strong> If you do not reset your password within this time, you will need to make a new request.
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
              &copy; {DateTime.Now.Year} HRM System. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

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

            string subject = "HRM System – Password Reset Requests";
            
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
  <title>HRM System – Password Reset Requests</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f4f4; padding: 20px 0;"">
    <tr>
      <td align=""center"">
        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dee2e6;"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #0d6efd; padding: 30px 40px;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 HRM System</h1>
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
                    ⏰ <strong>These links will expire in {expiryMinutes} minutes.</strong> If you do not reset your password within this time, you will need to make a new request.
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
              &copy; {DateTime.Now.Year} HRM System. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

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

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

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
            }
        }

        public async Task SendWelcomeAndSetupEmailAsync(string toEmail, string toName, string username, string setupLink, int expiryMinutes)
        {
            var (fromEmail, fromName, smtpUsername, smtpPassword, smtpHost, smtpPort, enableSsl) = GetSmtpConfig();

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
              <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">🏢 HRM System</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 40px; color: #333333;"">
              <h2 style=""color: #1e3c72; margin-top: 0; margin-bottom: 20px; font-size: 20px;"">Welcome to the Team!</h2>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 16px 0;"">Hi <strong>{toName}</strong>,</p>
              <p style=""font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">Your employee profile has been registered in the HRM System. To get started, please set up your password and complete your profile.</p>

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
                    ⏰ <strong>This setup link will expire in {expiryMinutes} minutes.</strong>
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
              &copy; {DateTime.Now.Year} HRM System. This is an automated email, please do not reply.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

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
    }
}
