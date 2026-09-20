using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin", "Manager", "Employee")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userRoles = HttpContext.Session.GetString("UserRoles") ?? "";

            if (userRoles.Contains("Admin"))
            {
                var payrolls = await _context.PayrollRecords
                    .Include(p => p.Employee)
                    .OrderByDescending(p => p.Year)
                    .ThenByDescending(p => p.Month)
                    .ToListAsync();
                return View("AdminIndex", payrolls);
            }
            else
            {
                var empIdStr = HttpContext.Session.GetString("EmployeeId");
                if (int.TryParse(empIdStr, out int empId))
                {
                    var payrolls = await _context.PayrollRecords
                        .Where(p => p.EmployeeId == empId)
                        .OrderByDescending(p => p.Year)
                        .ThenByDescending(p => p.Month)
                        .ToListAsync();
                    return View("MyPayroll", payrolls);
                }
                return Unauthorized();
            }
        }

        [AuthorizeRole("Admin")]
        [HttpGet]
        public IActionResult Generate()
        {
            ViewBag.Employees = new SelectList(_context.Employees.Where(e => e.IsActive), "Id", "FullName");
            return View();
        }

        [AuthorizeRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(PayrollRecord payroll)
        {
            var exists = await _context.PayrollRecords
                .AnyAsync(p => p.EmployeeId == payroll.EmployeeId && p.Month == payroll.Month && p.Year == payroll.Year);

            if (exists)
            {
                ModelState.AddModelError("", "Payroll for this month and year has already been generated for this employee.");
            }

            if (ModelState.IsValid)
            {
                payroll.NetSalary = payroll.BasicSalary + payroll.Allowances - payroll.Deductions;
                payroll.GeneratedAt = DateTime.Now;
                payroll.Status = PayrollStatus.Processed;

                _context.Add(payroll);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payroll generated successfully.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.Employees = new SelectList(_context.Employees.Where(e => e.IsActive), "Id", "FullName", payroll.EmployeeId);
            return View(payroll);
        }
        
        [AuthorizeRole("Admin")]
        [HttpPost]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var payroll = await _context.PayrollRecords.FindAsync(id);
            if (payroll != null)
            {
                payroll.Status = PayrollStatus.Paid;
                payroll.PaidAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payroll marked as paid.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PaySlip(int id)
        {
            var userRoles = HttpContext.Session.GetString("UserRoles") ?? "";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            int.TryParse(empIdStr ?? "0", out int currentEmpId);

            var payroll = await _context.PayrollRecords
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payroll == null) return NotFound();

            // Security check: Only Admins can see any payslip; others only their own.
            if (!userRoles.Contains("Admin") && payroll.EmployeeId != currentEmpId)
            {
                return Unauthorized();
            }

            return View(payroll);
        }
    }
}
