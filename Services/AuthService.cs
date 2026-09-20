using HRMSystem.Data;
using HRMSystem.Models;
using Microsoft.EntityFrameworkCore;
using HRMSystem.Helpers;

namespace HRMSystem.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                                     .Include(u => u.Employee)
                                     .Include(u => u.UserRoles)
                                        .ThenInclude(ur => ur.Role)
                                     .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null) return null;

            if (PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return user;
            }

            return null;
        }

        public async Task<bool> ProcessFirstLoginResetAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.MustChangePassword) return false;

            if (!PasswordHelper.VerifyPassword(oldPassword, user.PasswordHash)) return false;

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(User user, string rawPassword)> CreateEmployeeAccountAsync(int employeeId, string username, List<int>? selectedRoles = null)
        {
            string rawPassword = PasswordHelper.GenerateRandomPassword();
            
            var roleMappings = new List<UserRoleMapping>();
            if (selectedRoles != null && selectedRoles.Any())
            {
                foreach(var roleId in selectedRoles)
                {
                    roleMappings.Add(new UserRoleMapping { RoleId = roleId });
                }
            }
            else
            {
                roleMappings.Add(new UserRoleMapping { RoleId = 4 }); // Fallback to bare Employee
            }

            var user = new User
            {
                EmployeeId = employeeId,
                Username = username,
                PasswordHash = PasswordHelper.HashPassword(rawPassword),
                MustChangePassword = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UserRoles = roleMappings
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (user, rawPassword);
        }

        /// <summary>
        /// Generates a secure password-reset token for the user with the given employee email.
        /// Returns (token string, user) or null if email not found.
        /// </summary>
        public async Task<(string token, User user)?> GeneratePasswordResetTokenAsync(string email)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower());
            if (employee == null) return null;

            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.EmployeeId == employee.Id && u.IsActive);

            if (user == null) return null;

            int expiryMinutes = _config.GetValue<int>("EmailSettings:TokenExpiryMinutes", 30);

            // Invalidate any existing unused tokens for this user
            var existing = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();
            foreach (var t in existing) t.IsUsed = true;

            var token = Guid.NewGuid().ToString("N"); // 32-char hex string
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(expiryMinutes),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            return (token, user);
        }

        /// <summary>
        /// Generates secure password-reset tokens for all active users matching the given email.
        /// </summary>
        public async Task<List<(string token, User user)>> GenerateAllPasswordResetTokensAsync(string email)
        {
            var results = new List<(string token, User user)>();

            // Find all active users linked to employees with this email
            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.IsActive && u.Employee != null && u.Employee.Email.ToLower() == email.ToLower())
                .ToListAsync();

            if (users == null || !users.Any()) return results;

            int expiryMinutes = _config.GetValue<int>("EmailSettings:TokenExpiryMinutes", 30);

            foreach (var user in users)
            {
                // Invalidate any existing unused tokens for this user
                var existing = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed)
                    .ToListAsync();
                foreach (var t in existing) t.IsUsed = true;

                var token = Guid.NewGuid().ToString("N");
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddMinutes(expiryMinutes),
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };

                _context.PasswordResetTokens.Add(resetToken);
                results.Add((token, user));
            }

            await _context.SaveChangesAsync();
            return results;
        }

        /// <summary>
        /// Validates the reset token. Returns the associated User if valid, null if expired/invalid/used.
        /// </summary>
        public async Task<User?> ValidateResetTokenAsync(string token)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.Now)
                return null;

            return resetToken.User;
        }

        /// <summary>
        /// Completes the password reset: sets new password, marks token used.
        /// </summary>
        public async Task<bool> CompletePasswordResetAsync(string token, string newPassword)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.Now)
                return false;

            resetToken.User!.PasswordHash = PasswordHelper.HashPassword(newPassword);
            resetToken.User.MustChangePassword = false;
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
