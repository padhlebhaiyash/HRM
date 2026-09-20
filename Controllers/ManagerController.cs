using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Manager", "HR", "Admin")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var manager = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.Id == empId);

            if (manager == null) return NotFound();

            // Employees who report to this manager
            var subordinates = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.User)
                    .ThenInclude(u => u!.UserRoles!)
                        .ThenInclude(ur => ur.Role)
                .Where(e => e.ManagerId == empId && e.IsActive)
                .OrderBy(e => e.FirstName)
                .ToListAsync();

            // Pending leave requests from subordinates
            var subordinateIds = subordinates.Select(e => e.Id).ToList();
            var pendingLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => subordinateIds.Contains(l.EmployeeId) && l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.AppliedAt)
                .ToListAsync();

            // Recent leaves from team (last 30 days)
            var recentLeaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => subordinateIds.Contains(l.EmployeeId) && l.AppliedAt >= DateTime.Now.AddDays(-30))
                .OrderByDescending(l => l.AppliedAt)
                .Take(15)
                .ToListAsync();

            // Subordinate Leave Balances for team summary
            var teamBalances = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .Include(lb => lb.LeaveType)
                .Where(lb => subordinateIds.Contains(lb.EmployeeId))
                .OrderBy(lb => lb.Employee!.FirstName)
                .ToListAsync();

            ViewBag.Manager = manager;
            ViewBag.Subordinates = subordinates;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.RecentLeaves = recentLeaves;
            ViewBag.TeamBalances = teamBalances;
            ViewBag.TeamSize = subordinates.Count;

            return View();
        }

        // Manager view — see all leaves for their team
        public async Task<IActionResult> TeamLeaves()
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var subordinateIds = await _context.Employees
                .Where(e => e.ManagerId == empId && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync();

            var leaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => subordinateIds.Contains(l.EmployeeId))
                .OrderByDescending(l => l.AppliedAt)
                .ToListAsync();

            return View(leaves);
        }
    }
}
