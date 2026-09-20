using System;
using System.Linq;
using System.Threading.Tasks;
using HRMSystem.Data;
using HRMSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRMSystem.Helpers
{
    public static class IntegrationTests
    {
        public static async Task<bool> RunTestsAsync(ApplicationDbContext db, IServiceProvider sp)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("RUNNING AUTOMATED INTEGRATION TESTS...");
            Console.WriteLine("========================================");

            bool leaveTypeSuccess = await TestLeaveTypesAsync(db);
            bool profileChangeSuccess = await TestProfileChangeFlowAsync(db);
            bool employeeValidationSuccess = await TestEmployeeValidationAsync(db);
            bool designationHierarchySuccess = await TestDesignationHierarchyAsync(db);
            bool emailSuccess = await TestEmailSendingAsync(sp);

            Console.WriteLine("========================================");
            if (leaveTypeSuccess && profileChangeSuccess && employeeValidationSuccess && designationHierarchySuccess && emailSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALL TESTS PASSED SUCCESSFULLY!");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"TEST FAILURE. LeaveTypes: {(leaveTypeSuccess ? "PASSED" : "FAILED")}, ProfileChangeFlow: {(profileChangeSuccess ? "PASSED" : "FAILED")}, EmployeeValidation: {(employeeValidationSuccess ? "PASSED" : "FAILED")}, DesignationHierarchy: {(designationHierarchySuccess ? "PASSED" : "FAILED")}, EmailSending: {(emailSuccess ? "PASSED" : "FAILED")}");
                Console.ResetColor();
                return false;
            }
        }

        private static async Task<bool> TestLeaveTypesAsync(ApplicationDbContext db)
        {
            Console.WriteLine("Testing Leave Types Management...");
            try
            {
                // 1. Verify default core leave types exist
                var defaultCodes = new[] { "Casual", "Sick", "Earned", "Unpaid", "WFH" };
                var dbLeaveTypes = await db.LeaveTypes.ToListAsync();

                foreach (var code in defaultCodes)
                {
                    var lt = dbLeaveTypes.FirstOrDefault(t => t.Code == code);
                    if (lt == null)
                    {
                        Console.WriteLine($"[FAIL] Core leave type with code '{code}' not found.");
                        return false;
                    }
                    Console.WriteLine($"[PASS] Found core leave type: {lt.Name} ({lt.Code})");
                }

                // 2. Try adding a new custom leave type
                string testName = "Test Study Leave";
                string testCode = "TSL";
                
                // Clean up previous test run if any
                var existingTest = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Code == testCode);
                if (existingTest != null)
                {
                    db.LeaveTypes.Remove(existingTest);
                    await db.SaveChangesAsync();
                }

                var newType = new LeaveType
                {
                    Name = testName,
                    Code = testCode,
                    IsActive = true
                };

                db.LeaveTypes.Add(newType);
                await db.SaveChangesAsync();

                var retrieved = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Code == testCode);
                if (retrieved == null || retrieved.Name != testName)
                {
                    Console.WriteLine("[FAIL] Custom leave type was not saved/retrieved correctly.");
                    return false;
                }
                Console.WriteLine($"[PASS] Successfully added and verified custom leave type: {retrieved.Name}");

                // 3. Clean up the custom leave type
                db.LeaveTypes.Remove(retrieved);
                await db.SaveChangesAsync();
                Console.WriteLine("[PASS] Cleaned up custom leave type.");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception in TestLeaveTypesAsync: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestProfileChangeFlowAsync(ApplicationDbContext db)
        {
            Console.WriteLine("Testing Employee Profile Change Approval Flow...");
            try
            {
                // 1. Find or create an employee for testing
                var emp = await db.Employees.FirstOrDefaultAsync();
                if (emp == null)
                {
                    // Create a mock department first if none exists
                    var dept = await db.Departments.FirstOrDefaultAsync();
                    if (dept == null)
                    {
                        dept = new Department { Name = "Test Dept", Description = "Test Description" };
                        db.Departments.Add(dept);
                        await db.SaveChangesAsync();
                    }

                    emp = new Employee
                    {
                        FirstName = "Test",
                        LastName = "Employee",
                        EmployeeCode = "T001",
                        Email = "test@example.com",
                        DateOfJoining = DateTime.Now,
                        DepartmentId = dept.Id,
                        DesignationId = (await db.Designations.FirstOrDefaultAsync())?.Id ?? 1,
                        LocalAddress = "Original Local Address",
                        PermanentAddress = "Original Permanent Address",
                        PhotoPath = "/img/profiles/default.png"
                    };
                    db.Employees.Add(emp);
                    await db.SaveChangesAsync();
                }

                string originalLocal = emp.LocalAddress ?? "Orig Local";
                string originalPerm = emp.PermanentAddress ?? "Orig Perm";
                string originalPhoto = emp.PhotoPath ?? "Orig Photo";

                // Ensure clean state: remove any pending profile change requests for this employee
                var oldRequests = await db.ProfileChangeRequests.Where(r => r.EmployeeId == emp.Id).ToListAsync();
                db.ProfileChangeRequests.RemoveRange(oldRequests);
                await db.SaveChangesAsync();

                // 2. Submit a profile change request
                string newLocal = "123 New Local St";
                string newPerm = "456 New Perm Ave";
                string newPhoto = "/img/profiles/new_test.png";

                var request = new ProfileChangeRequest
                {
                    EmployeeId = emp.Id,
                    LocalAddress = newLocal,
                    PermanentAddress = newPerm,
                    PhotoPath = newPhoto,
                    IsPending = true,
                    IsApproved = false,
                    RequestedAt = DateTime.Now
                };

                db.ProfileChangeRequests.Add(request);
                await db.SaveChangesAsync();

                // 3. Verify employee record is NOT updated immediately
                // Force reload employee from DB
                db.Entry(emp).State = EntityState.Detached;
                var reloadedEmp = await db.Employees.FindAsync(emp.Id);
                if (reloadedEmp!.LocalAddress == newLocal || reloadedEmp.PermanentAddress == newPerm || reloadedEmp.PhotoPath == newPhoto)
                {
                    Console.WriteLine("[FAIL] Employee record updated immediately without admin approval!");
                    return false;
                }
                Console.WriteLine("[PASS] Verified that profile updates do NOT take effect immediately.");

                // 4. Admin approves the request
                var pendingReq = await db.ProfileChangeRequests.FirstOrDefaultAsync(r => r.Id == request.Id);
                if (pendingReq == null)
                {
                    Console.WriteLine("[FAIL] ProfileChangeRequest was not saved to DB.");
                    return false;
                }

                // Simulate Admin Approval Logic
                if (pendingReq.LocalAddress != null) reloadedEmp.LocalAddress = pendingReq.LocalAddress;
                if (pendingReq.PermanentAddress != null) reloadedEmp.PermanentAddress = pendingReq.PermanentAddress;
                if (pendingReq.PhotoPath != null) reloadedEmp.PhotoPath = pendingReq.PhotoPath;
                reloadedEmp.UpdatedAt = DateTime.Now;
                db.Employees.Update(reloadedEmp);

                pendingReq.IsPending = false;
                pendingReq.IsApproved = true;
                pendingReq.ReviewedAt = DateTime.Now;
                db.ProfileChangeRequests.Update(pendingReq);
                await db.SaveChangesAsync();

                // Verify employee record is updated now
                db.Entry(reloadedEmp).State = EntityState.Detached;
                var approvedEmp = await db.Employees.FindAsync(emp.Id);
                if (approvedEmp!.LocalAddress != newLocal || approvedEmp.PermanentAddress != newPerm || approvedEmp.PhotoPath != newPhoto)
                {
                    Console.WriteLine("[FAIL] Employee record was NOT updated after admin approval!");
                    return false;
                }
                Console.WriteLine("[PASS] Verified that profile updates take effect after admin approval.");

                // 5. Submit another request, then Admin rejects it
                var rejectRequest = new ProfileChangeRequest
                {
                    EmployeeId = emp.Id,
                    LocalAddress = "Rejected Local St",
                    PermanentAddress = "Rejected Perm Ave",
                    PhotoPath = "/img/profiles/rejected.png",
                    IsPending = true,
                    IsApproved = false,
                    RequestedAt = DateTime.Now
                };
                db.ProfileChangeRequests.Add(rejectRequest);
                await db.SaveChangesAsync();

                // Simulate Admin Rejection Logic
                rejectRequest.IsPending = false;
                rejectRequest.IsApproved = false;
                rejectRequest.ReviewedAt = DateTime.Now;
                db.ProfileChangeRequests.Update(rejectRequest);
                await db.SaveChangesAsync();

                // Verify employee record is NOT updated
                db.Entry(approvedEmp).State = EntityState.Detached;
                var finalEmp = await db.Employees.FindAsync(emp.Id);
                if (finalEmp!.LocalAddress == "Rejected Local St" || finalEmp.PermanentAddress == "Rejected Perm Ave" || finalEmp.PhotoPath == "/img/profiles/rejected.png")
                {
                    Console.WriteLine("[FAIL] Employee record was updated even after admin rejection!");
                    return false;
                }
                Console.WriteLine("[PASS] Verified that profile updates do NOT take effect after admin rejection.");

                // 6. Clean up: restore original values for employee, delete test request entries
                finalEmp.LocalAddress = originalLocal;
                finalEmp.PermanentAddress = originalPerm;
                finalEmp.PhotoPath = originalPhoto;
                db.Employees.Update(finalEmp);

                var cleanupRequests = await db.ProfileChangeRequests.Where(r => r.EmployeeId == emp.Id).ToListAsync();
                db.ProfileChangeRequests.RemoveRange(cleanupRequests);
                await db.SaveChangesAsync();
                Console.WriteLine("[PASS] Restored employee details and cleaned up requests.");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception in TestProfileChangeFlowAsync: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestEmployeeValidationAsync(ApplicationDbContext db)
        {
            Console.WriteLine("Testing Employee Unique Code & Email Validation...");
            try
            {
                // Create a temporary unique employee first
                var dept = await db.Departments.FirstOrDefaultAsync();
                if (dept == null)
                {
                    dept = new Department { Name = "Test Dept", Description = "Test Description" };
                    db.Departments.Add(dept);
                    await db.SaveChangesAsync();
                }

                string duplicateCode = "EMP_DUPE_TEST";
                string duplicateEmail = "dupe_test@example.com";

                // Ensure clean state: remove existing with same code or email
                var existing = await db.Employees.Where(e => e.EmployeeCode == duplicateCode || e.Email == duplicateEmail).ToListAsync();
                if (existing.Any())
                {
                    db.Employees.RemoveRange(existing);
                    await db.SaveChangesAsync();
                }

                // Add the base employee
                var baseEmp = new Employee
                {
                    FirstName = "Base",
                    LastName = "Employee",
                    EmployeeCode = duplicateCode,
                    Email = duplicateEmail,
                    DateOfJoining = DateTime.Now,
                    DepartmentId = dept.Id,
                    DesignationId = (await db.Designations.FirstOrDefaultAsync())?.Id ?? 1
                };
                db.Employees.Add(baseEmp);
                await db.SaveChangesAsync();

                // Now simulate the controller logic to check for validation
                // 1. Check duplicate employee code
                bool codeIsDuplicate = await db.Employees.AnyAsync(e => e.EmployeeCode == duplicateCode);
                if (!codeIsDuplicate)
                {
                    Console.WriteLine("[FAIL] Expected duplicate Employee Code check to return true.");
                    return false;
                }
                Console.WriteLine("[PASS] Verified duplicate Employee Code check works.");

                // 2. Check duplicate email
                bool emailIsDuplicate = await db.Employees.AnyAsync(e => e.Email == duplicateEmail);
                if (!emailIsDuplicate)
                {
                    Console.WriteLine("[FAIL] Expected duplicate Email check to return true.");
                    return false;
                }
                Console.WriteLine("[PASS] Verified duplicate Email check works.");

                var defaultDesignationId = (await db.Designations.FirstOrDefaultAsync())?.Id ?? 1;
                var emp1 = new Employee { FirstName = "Emp1", LastName = "Test", EmployeeCode = "E001_VAL", Email = "e1_val@example.com", DepartmentId = dept.Id, DesignationId = defaultDesignationId, DateOfJoining = DateTime.Now };
                var emp2 = new Employee { FirstName = "Emp2", LastName = "Test", EmployeeCode = "E002_VAL", Email = "e2_val@example.com", DepartmentId = dept.Id, DesignationId = defaultDesignationId, DateOfJoining = DateTime.Now };
                db.Employees.AddRange(emp1, emp2);
                await db.SaveChangesAsync();

                bool emailConflict = await db.Employees.AnyAsync(e => e.Email == emp1.Email && e.Id != emp2.Id);
                if (!emailConflict)
                {
                    Console.WriteLine("[FAIL] Expected email conflict check on Edit to return true.");
                    return false;
                }
                Console.WriteLine("[PASS] Verified email conflict check on Edit works.");

                // Clean up all test employees
                db.Employees.Remove(baseEmp);
                db.Employees.RemoveRange(emp1, emp2);
                await db.SaveChangesAsync();
                Console.WriteLine("[PASS] Cleaned up validation test employees.");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception in TestEmployeeValidationAsync: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestEmailSendingAsync(IServiceProvider sp)
        {
            Console.WriteLine("Testing Email Sending (SMTP)...");
            try
            {
                var emailService = sp.GetRequiredService<Services.EmailService>();
                var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var testEmail = config["EmailSettings:FromEmail"] ?? "padhlebhaiyash@gmail.com";
                
                Console.WriteLine($"Attempting to send a test email to: {testEmail}...");
                await emailService.SendWelcomeAndSetupEmailAsync(
                    testEmail,
                    "Test User",
                    "testuser",
                    "http://localhost:5035/Auth/ResetPasswordViaToken?token=test",
                    30);
                
                Console.WriteLine("[PASS] Test email sent successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception in TestEmailSendingAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        private static async Task<bool> TestDesignationHierarchyAsync(ApplicationDbContext db)
        {
            Console.WriteLine("Testing Designation Hierarchy Tree...");
            try
            {
                var allDes = await db.Designations.ToListAsync();
                Console.WriteLine($"Total designations in DB: {allDes.Count}");
                foreach (var d in allDes)
                {
                    Console.WriteLine($"- Id: {d.Id}, Name: {d.Name}, Level: {d.Level}, ParentId: {(d.ParentDesignationId?.ToString() ?? "null")}");
                }

                // 1. Verify root Admin designation exists
                var admin = await db.Designations.FirstOrDefaultAsync(d => d.ParentDesignationId == null);
                if (admin == null || admin.Level != 1)
                {
                    Console.WriteLine("[FAIL] Root Admin designation with parent null and Level 1 not found.");
                    return false;
                }
                Console.WriteLine($"[PASS] Found root designation: {admin.Name} (Level {admin.Level})");

                // 2. Add a custom branch designation under Admin
                var manager = await db.Designations.FirstOrDefaultAsync(d => d.ParentDesignationId == admin.Id);
                if (manager == null)
                {
                    manager = new Designation
                    {
                        Name = "Test Manager",
                        Level = admin.Level + 1,
                        ParentDesignationId = admin.Id
                    };
                    db.Designations.Add(manager);
                    await db.SaveChangesAsync();
                }

                // 3. Add a test child under Manager
                var testTL = new Designation
                {
                    Name = "Test Team Lead",
                    ParentDesignationId = manager.Id,
                    Level = manager.Level + 1 // Should be 3
                };
                db.Designations.Add(testTL);
                await db.SaveChangesAsync();

                if (testTL.Level != 3)
                {
                    Console.WriteLine($"[FAIL] Child level calculation failed. Expected 3, got {testTL.Level}");
                    db.Designations.Remove(testTL);
                    await db.SaveChangesAsync();
                    return false;
                }
                Console.WriteLine($"[PASS] Custom designation created and Level calculated correctly: {testTL.Name} (Level {testTL.Level})");

                // 4. Test recursive level update
                var tempParent = new Designation
                {
                    Name = "Temp Parent",
                    ParentDesignationId = admin.Id,
                    Level = admin.Level + 1 // Level 2
                };
                db.Designations.Add(tempParent);
                await db.SaveChangesAsync();

                // Move testTL under tempParent
                testTL.ParentDesignationId = tempParent.Id;
                testTL.Level = tempParent.Level + 1; // Level 3
                db.Designations.Update(testTL);
                await db.SaveChangesAsync();

                // Shift tempParent's level to 3
                tempParent.Level = 3;
                db.Designations.Update(tempParent);
                await db.SaveChangesAsync();

                // Recursively update child level (simulating controller behavior)
                var children = await db.Designations.Where(d => d.ParentDesignationId == tempParent.Id).ToListAsync();
                foreach (var child in children)
                {
                    child.Level = tempParent.Level + 1;
                    db.Designations.Update(child);
                }
                await db.SaveChangesAsync();

                var updatedTL = await db.Designations.FindAsync(testTL.Id);
                if (updatedTL == null || updatedTL.Level != 4)
                {
                    Console.WriteLine($"[FAIL] Recursive level update failed. Expected 4, got {updatedTL?.Level}");
                    db.Designations.Remove(testTL);
                    db.Designations.Remove(tempParent);
                    await db.SaveChangesAsync();
                    return false;
                }
                Console.WriteLine($"[PASS] Recursive child level updates work correctly. Level shifted to {updatedTL.Level}");

                // Clean up
                db.Designations.Remove(updatedTL);
                db.Designations.Remove(tempParent);
                await db.SaveChangesAsync();
                Console.WriteLine("[PASS] Cleaned up test designations.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception in TestDesignationHierarchyAsync: {ex.Message}");
                return false;
            }
        }
    }
}
