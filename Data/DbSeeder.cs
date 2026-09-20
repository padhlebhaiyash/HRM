using HRMSystem.Helpers;
using HRMSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Data
{
    public static class DbSeeder
    {
        public static void SeedData(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure database is created and migrations applied
                context.Database.Migrate();

                // Auto-heal designations parent-child tree mapping
                var rootAdmin = context.Designations.FirstOrDefault(d => d.Name == "Admin");
                if (rootAdmin != null)
                {
                    bool changed = false;
                    if (rootAdmin.ParentDesignationId != null || rootAdmin.Level != 1)
                    {
                        rootAdmin.ParentDesignationId = null;
                        rootAdmin.Level = 1;
                        context.Designations.Update(rootAdmin);
                        changed = true;
                    }

                    var otherRoots = context.Designations
                        .Where(d => d.Id != rootAdmin.Id && d.ParentDesignationId == null)
                        .ToList();

                    foreach (var or in otherRoots)
                    {
                        or.ParentDesignationId = rootAdmin.Id;
                        context.Designations.Update(or);
                        changed = true;
                    }

                    if (changed)
                    {
                        context.SaveChanges();
                    }

                    // Recalculate all levels recursively
                    void RecalculateLevels(int parentId, int parentLevel)
                    {
                        var children = context.Designations.Where(d => d.ParentDesignationId == parentId).ToList();
                        foreach (var child in children)
                        {
                            if (child.Level != parentLevel + 1)
                            {
                                child.Level = parentLevel + 1;
                                context.Designations.Update(child);
                            }
                            RecalculateLevels(child.Id, child.Level);
                        }
                    }

                    RecalculateLevels(rootAdmin.Id, rootAdmin.Level);
                    context.SaveChanges();
                }

                if (!context.Users.Any())
                {
                    // Ensure Administration department exists
                    var adminDept = context.Departments.FirstOrDefault(d => d.Name == "Administration");
                    if (adminDept == null)
                    {
                        adminDept = new Department
                        {
                            Name = "Administration",
                            Description = "System Administration Department",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        context.Departments.Add(adminDept);
                        context.SaveChanges();
                    }

                    // Ensure Admin designation exists
                    var adminDesignation = context.Designations.FirstOrDefault(d => d.Name == "Admin");
                    if (adminDesignation == null)
                    {
                        adminDesignation = new Designation
                        {
                            Name = "Admin",
                            Level = 1
                        };
                        context.Designations.Add(adminDesignation);
                        context.SaveChanges();
                    }

                    // Create System Admin Employee
                    var adminEmployee = new Employee
                    {
                        FirstName = "System",
                        LastName = "Admin",
                        EmployeeCode = "EMP000",
                        Email = "admin@hrmsystem.com",
                        DateOfJoining = DateTime.Now.AddYears(-1),
                        DepartmentId = adminDept.Id,
                        DesignationId = adminDesignation.Id,
                        Salary = 100000,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    context.Employees.Add(adminEmployee);
                    context.SaveChanges();

                    // Fresh DB — create the admin user with role mapping linked to Admin Employee
                    var adminUser = new User
                    {
                        EmployeeId = adminEmployee.Id,
                        Username = "admin",
                        PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                        MustChangePassword = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UserRoles = new List<UserRoleMapping>
                        {
                            new UserRoleMapping { RoleId = 1 } // Admin
                        }
                    };
                    context.Users.Add(adminUser);
                    context.SaveChanges();
                }
                else
                {
                    // Patch existing users that have no role mappings (migration from old enum system)
                    var usersWithoutRoles = context.Users
                        .Where(u => !context.UserRoles.Any(ur => ur.UserId == u.Id))
                        .ToList();

                    foreach (var user in usersWithoutRoles)
                    {
                        // The system admin account gets Admin role; all others get Employee role
                        int roleId = user.Username == "admin" ? 1 : 4;
                        context.UserRoles.Add(new UserRoleMapping { UserId = user.Id, RoleId = roleId });
                    }

                    if (usersWithoutRoles.Any())
                        context.SaveChanges();
                }

                // Ensure CompanySettings has default domain and email settings
                try
                {
                    var companySetting = context.CompanySettings.FirstOrDefault();
                    if (companySetting == null)
                    {
                        companySetting = new CompanySetting
                        {
                            CompanyName = null,
                            AppBaseUrl = "https://munrohr.inovexa.solutions",
                            FromEmail = "techinovexasolutions@gmail.com",
                            FromName = "HRM System"
                        };
                        context.CompanySettings.Add(companySetting);
                        context.SaveChanges();
                    }
                    else
                    {
                        bool updated = false;
                        if (string.IsNullOrWhiteSpace(companySetting.AppBaseUrl))
                        {
                            companySetting.AppBaseUrl = "https://munrohr.inovexa.solutions";
                            updated = true;
                        }
                        if (string.IsNullOrWhiteSpace(companySetting.FromEmail))
                        {
                            companySetting.FromEmail = "techinovexasolutions@gmail.com";
                            updated = true;
                        }
                        if (string.IsNullOrWhiteSpace(companySetting.FromName))
                        {
                            companySetting.FromName = "HRM System";
                            updated = true;
                        }
                        if (updated)
                        {
                            context.CompanySettings.Update(companySetting);
                            context.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] DbSeeder CompanySettings warning: {ex.Message}");
                }

                // Remove test employee Nita (EMP005) if exists
                try
                {
                    var nita = context.Employees.FirstOrDefault(e => e.EmployeeCode == "EMP005");
                    if (nita != null)
                    {
                        var nitaUser = context.Users.FirstOrDefault(u => u.EmployeeId == nita.Id);
                        if (nitaUser != null)
                        {
                            var userRoles = context.UserRoles.Where(ur => ur.UserId == nitaUser.Id).ToList();
                            context.UserRoles.RemoveRange(userRoles);
                            context.Users.Remove(nitaUser);
                        }
                        context.Employees.Remove(nita);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] DbSeeder remove Nita warning: {ex.Message}");
                }
            }
        }
    }
}
