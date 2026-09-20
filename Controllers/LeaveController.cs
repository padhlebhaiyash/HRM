using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin", "Employee", "Manager", "HR")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sessionRoles = HttpContext.Session.GetString("UserRoles") ?? "";
            
            if (sessionRoles.Contains("Admin"))
            {
                var leaves = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .Include(l => l.LeaveType)
                    .OrderByDescending(l => l.AppliedAt)
                    .ToListAsync();
                return View("AdminIndex", leaves);
            }
            else
            {
                var empIdStr = HttpContext.Session.GetString("EmployeeId");
                if (int.TryParse(empIdStr, out int empId))
                {
                    var leaves = await _context.LeaveRequests
                        .Include(l => l.LeaveType)
                        .Where(l => l.EmployeeId == empId)
                        .OrderByDescending(l => l.AppliedAt)
                        .ToListAsync();
                    return View("MyLeaves", leaves);
                }
                return Unauthorized();
            }
        }

        [AuthorizeRole("Employee")]
        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            ViewData["LeaveTypeId"] = new SelectList(leaveTypes, "Id", "Name");
            return View();
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequest leave)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (int.TryParse(empIdStr, out int empId))
            {
                if (ModelState.IsValid)
                {
                    if (leave.EndDate < leave.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
                        var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
                        ViewData["LeaveTypeId"] = new SelectList(leaveTypes, "Id", "Name", leave.LeaveTypeId);
                        return View(leave);
                    }

                    // Overlapping check specifically for Pending and Approved leaves
                    var hasOverlap = await _context.LeaveRequests
                        .AnyAsync(l => l.EmployeeId == empId 
                                  && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
                                  && l.StartDate <= leave.EndDate 
                                  && l.EndDate >= leave.StartDate);
                    
                    if (hasOverlap)
                    {
                        ModelState.AddModelError(string.Empty, "You already have a Pending or Approved leave request during these dates.");
                        var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
                        ViewData["LeaveTypeId"] = new SelectList(leaveTypes, "Id", "Name", leave.LeaveTypeId);
                        return View(leave);
                    }

                    leave.EmployeeId = empId;
                    leave.AppliedAt = DateTime.Now;
                    leave.Status = LeaveStatus.Pending;

                    _context.Add(leave);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Leave request submitted successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }
            var activeLeaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            ViewData["LeaveTypeId"] = new SelectList(activeLeaveTypes, "Id", "Name", leave.LeaveTypeId);
            return View(leave);
        }

        [AuthorizeRole("Admin", "Manager", "HR")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string redirectTo = "")
        {
            return await UpdateStatus(id, LeaveStatus.Approved, redirectTo);
        }

        [AuthorizeRole("Admin", "Manager", "HR")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string remark, string redirectTo = "")
        {
            if (string.IsNullOrWhiteSpace(remark))
            {
                TempData["ErrorMessage"] = "Rejection remark is required.";
                return string.IsNullOrEmpty(redirectTo)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(redirectTo, "Manager");
            }
            return await UpdateStatus(id, LeaveStatus.Rejected, redirectTo, remark);
        }

        [AuthorizeRole("Admin", "Manager", "HR")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string redirectTo = "")
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave == null) return NotFound();

            // Security: non-admins can only delete their subordinates' leaves
            var sessionRoles = HttpContext.Session.GetString("UserRoles") ?? "";
            if (!sessionRoles.Contains("Admin"))
            {
                var empIdStr = HttpContext.Session.GetString("EmployeeId");
                if (!int.TryParse(empIdStr, out int managerId)) return Forbid();

                var isSubordinate = await _context.Employees
                    .AnyAsync(e => e.Id == leave.EmployeeId && e.ManagerId == managerId);
                if (!isSubordinate) return Forbid();
            }

            // Refund balance if approved leave is deleted
            if (leave.Status == LeaveStatus.Approved)
            {
                var balance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId && b.LeaveTypeId == leave.LeaveTypeId);
                if (balance != null)
                {
                    balance.Used -= leave.TotalDays;
                    _context.LeaveBalances.Update(balance);
                }
            }

            _context.LeaveRequests.Remove(leave);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request deleted. Balance refunded if applicable.";
            return string.IsNullOrEmpty(redirectTo)
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(redirectTo, "Manager");
        }

        private async Task<IActionResult> UpdateStatus(int id, LeaveStatus newStatus, string redirectTo = "", string? remark = null)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave == null) return NotFound();

            // Security: non-admins can only update leaves of their subordinates
            var sessionRoles = HttpContext.Session.GetString("UserRoles") ?? "";
            if (!sessionRoles.Contains("Admin"))
            {
                var empIdStr = HttpContext.Session.GetString("EmployeeId");
                if (!int.TryParse(empIdStr, out int managerId)) return Forbid();

                var isSubordinate = await _context.Employees
                    .AnyAsync(e => e.Id == leave.EmployeeId && e.ManagerId == managerId);
                if (!isSubordinate) return Forbid();
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out int userId))
            {
                var balance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId && b.LeaveTypeId == leave.LeaveTypeId);

                if (balance == null)
                {
                    balance = new LeaveBalance
                    {
                        EmployeeId = leave.EmployeeId,
                        LeaveTypeId = leave.LeaveTypeId,
                        Allocated = 0,
                        Used = 0
                    };
                    _context.LeaveBalances.Add(balance);
                }

                if (leave.Status != LeaveStatus.Approved && newStatus == LeaveStatus.Approved)
                    balance.Used += leave.TotalDays;
                else if (leave.Status == LeaveStatus.Approved && newStatus != LeaveStatus.Approved)
                    balance.Used -= leave.TotalDays;

                leave.Status = newStatus;
                leave.ActionAt = DateTime.Now;
                leave.ApprovedById = userId;
                if (newStatus == LeaveStatus.Rejected)
                    leave.RejectionRemark = remark;

                _context.Update(leave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Leave request {newStatus}.";
            }

            return string.IsNullOrEmpty(redirectTo)
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(redirectTo, "Manager");
        }
    }
}
