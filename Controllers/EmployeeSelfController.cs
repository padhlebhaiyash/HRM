using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using HRMSystem.Services;
using System.Linq;

namespace HRMSystem.Controllers
{
    public class EmployeeSelfController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public EmployeeSelfController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> Dashboard()
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.Id == empId);

            if (employee == null) return NotFound();

            // Seed Leave Balances for POC if they don't exist
            var balances = await _context.LeaveBalances.Include(b => b.LeaveType).Where(lb => lb.EmployeeId == empId).ToListAsync();
            var activeLeaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive).ToListAsync();
            bool balancesChanged = false;
            
            foreach (var lt in activeLeaveTypes)
            {
                if (!balances.Any(b => b.LeaveTypeId == lt.Id))
                {
                    var defaultAlloc = lt.Code switch
                    {
                        "Casual" => 6m,
                        "Sick" => 6m,
                        "Earned" => 25m,
                        _ => 0m
                    };
                    var newBalance = new LeaveBalance
                    {
                        EmployeeId = empId,
                        LeaveTypeId = lt.Id,
                        Allocated = defaultAlloc,
                        Used = 0m
                    };
                    _context.LeaveBalances.Add(newBalance);
                    balances.Add(newBalance);
                    balancesChanged = true;
                }
            }
            if (balancesChanged)
            {
                await _context.SaveChangesAsync();
            }

            ViewBag.LeaveBalances = balances;

            // Recent leave requests for the dashboard panel
            ViewBag.RecentLeaves = await _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == empId)
                .OrderByDescending(l => l.AppliedAt)
                .Take(10)
                .ToListAsync();

            // Load birthdays dynamically for the dashboard
            var currentMonth = DateTime.Today.Month;
            ViewBag.Birthdays = await _context.Employees
                .Where(e => e.IsActive && e.DateOfBirth.HasValue && e.DateOfBirth.Value.Month == currentMonth)
                .OrderBy(e => e.DateOfBirth.Value.Day)
                .ToListAsync();

            // Load Today's Attendance and Active Office Locations for Geofenced Clock In/Out widget
            var today = DateTime.Today;
            ViewBag.TodayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);
            ViewBag.OfficeLocations = await _context.OfficeLocations
                .Where(l => l.IsActive)
                .ToListAsync();

            return View(employee);
        }

        [AuthorizeRole("Employee")]
        [HttpGet]
        public async Task<IActionResult> GetTeamAttendanceData(string start, string end)
        {
            if (!DateTime.TryParse(start, out DateTime startDate)) startDate = DateTime.Today.AddMonths(-1);
            if (!DateTime.TryParse(end, out DateTime endDate)) endDate = DateTime.Today.AddMonths(1);

            // Fetch approved leaves spanning this window for ALL employees to simulate Team Dashboard
            var leaves = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == LeaveStatus.Approved && l.StartDate <= endDate && l.EndDate >= startDate)
                .ToListAsync();

            var events = new List<object>();

            foreach (var leave in leaves)
            {
                // Assign colors based on LeaveType to match requirement: 
                string bgColor = leave.HalfDayPeriod != HalfDayPeriod.None ? "#c19b70" : leave.LeaveType?.Code switch
                {
                    "WFH" => "#2db84d",      // Distinct Green for WFH
                    _ => "#e50000"           // Bright Red for all other leaves (CL, SL, EL, Unpaid)
                };

                events.Add(new
                {
                    title = leave.Employee?.FirstName ?? "Unknown",
                    start = leave.StartDate.ToString("yyyy-MM-dd"),
                    // FullCalendar exclusive end date requires +1 day for inclusive visual ranges
                    end = leave.EndDate.AddDays(1).ToString("yyyy-MM-dd"),
                    backgroundColor = bgColor,
                    borderColor = bgColor,
                    textColor = "#fff",
                    allDay = true
                });
            }

            // Simulating "Office Off" for Weekends dynamically for the month
            for (DateTime dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    events.Add(new 
                    {
                        title = "🌲 Office Off",
                        start = dt.ToString("yyyy-MM-dd"),
                        display = "background",
                        backgroundColor = "#4a5a6a" // Dark grey like snapshot
                    });
                }
            }

            // Fetch holidays added by admin in this window
            var dbHolidays = await _context.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .ToListAsync();

            foreach (var holiday in dbHolidays)
            {
                // Background color for the day
                events.Add(new
                {
                    title = $"🎉 {holiday.Name}",
                    start = holiday.Date.ToString("yyyy-MM-dd"),
                    display = "background",
                    backgroundColor = holiday.Color
                });

                // Text label on the day
                events.Add(new
                {
                    title = $"🎉 Holiday: {holiday.Name}",
                    start = holiday.Date.ToString("yyyy-MM-dd"),
                    backgroundColor = "#ff9800", // Soft orange
                    borderColor = "#ff9800",
                    textColor = "#ffffff",
                    allDay = true
                });
            }

            return Json(events);
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> Profile()
        {
            ViewData["ActivePage"] = "Home";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(l => l.LeaveBalances)
                .ThenInclude(b => b.LeaveType)
                .FirstOrDefaultAsync(e => e.Id == empId);

            if (employee == null) return NotFound();

            // Ensure Bank Details exist for the view
            var bankDetail = await _context.BankDetails.FirstOrDefaultAsync(b => b.EmployeeId == empId);
            if (bankDetail == null)
            {
                bankDetail = new BankDetail { EmployeeId = empId };
                _context.BankDetails.Add(bankDetail);
                await _context.SaveChangesAsync();
            }

            ViewBag.BankDetail = bankDetail;
            
            // Seed Leave Balances for POC if they don't exist (reuse logic from Dashboard)
            var activeLeaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive).ToListAsync();
            bool balancesChanged = false;
            if (employee.LeaveBalances == null) employee.LeaveBalances = new List<LeaveBalance>();
            
            foreach (var lt in activeLeaveTypes)
            {
                if (!employee.LeaveBalances.Any(b => b.LeaveTypeId == lt.Id))
                {
                    var defaultAlloc = lt.Code switch
                    {
                        "Casual" => 6m,
                        "Sick" => 6m,
                        "Earned" => 25m,
                        _ => 0m
                    };
                    var newBalance = new LeaveBalance
                    {
                        EmployeeId = empId,
                        LeaveTypeId = lt.Id,
                        Allocated = defaultAlloc,
                        Used = 0m
                    };
                    _context.LeaveBalances.Add(newBalance);
                    employee.LeaveBalances.Add(newBalance);
                    balancesChanged = true;
                }
            }
            if (balancesChanged)
            {
                await _context.SaveChangesAsync();
            }

            // Load any pending profile change request for notice banner
            var pendingRequest = await _context.ProfileChangeRequests
                .FirstOrDefaultAsync(r => r.EmployeeId == empId && r.IsPending);
            ViewBag.PendingRequest = pendingRequest;

            // Load notices, holidays, and birthdays dynamically
            ViewBag.Notices = await _context.Notices.Where(n => n.IsActive).OrderByDescending(n => n.Date).ToListAsync();
            ViewBag.Holidays = await _context.Holidays.OrderBy(h => h.Date).ToListAsync();
            
            var currentMonth = DateTime.Today.Month;
            ViewBag.Birthdays = await _context.Employees
                .Where(e => e.IsActive && e.DateOfBirth.HasValue && e.DateOfBirth.Value.Month == currentMonth)
                .OrderBy(e => e.DateOfBirth.Value.Day)
                .ToListAsync();

            return View(employee);
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        public async Task<IActionResult> UpdatePersonalDetails(string fathersName, DateTime? dateOfBirth, Gender gender, string phone, string localAddress, string permanentAddress)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var employee = await _context.Employees.FindAsync(empId);
            if (employee == null) return NotFound();

            var pendingRequest = await _context.ProfileChangeRequests
                .FirstOrDefaultAsync(r => r.EmployeeId == empId && r.IsPending);

            if (pendingRequest == null)
            {
                pendingRequest = new ProfileChangeRequest
                {
                    EmployeeId = empId,
                    FathersName = fathersName,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    Phone = phone,
                    LocalAddress = localAddress,
                    PermanentAddress = permanentAddress,
                    PhotoPath = employee.PhotoPath,
                    IsPending = true,
                    RequestedAt = DateTime.Now
                };
                _context.ProfileChangeRequests.Add(pendingRequest);
            }
            else
            {
                pendingRequest.FathersName = fathersName;
                pendingRequest.DateOfBirth = dateOfBirth;
                pendingRequest.Gender = gender;
                pendingRequest.Phone = phone;
                pendingRequest.LocalAddress = localAddress;
                pendingRequest.PermanentAddress = permanentAddress;
                pendingRequest.RequestedAt = DateTime.Now;
                _context.ProfileChangeRequests.Update(pendingRequest);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your personal details change request has been submitted for Admin approval.";
            return RedirectToAction("Profile");
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        public async Task<IActionResult> UpdatePhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return BadRequest("No photo uploaded.");

            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var employee = await _context.Employees.FindAsync(empId);
            if (employee == null) return NotFound();

            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "profiles");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var fileName = $"profile_{empId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var photoPath = $"/img/profiles/{fileName}";

            var pendingRequest = await _context.ProfileChangeRequests
                .FirstOrDefaultAsync(r => r.EmployeeId == empId && r.IsPending);

            if (pendingRequest == null)
            {
                pendingRequest = new ProfileChangeRequest
                {
                    EmployeeId = empId,
                    LocalAddress = employee.LocalAddress,
                    PermanentAddress = employee.PermanentAddress,
                    PhotoPath = photoPath,
                    IsPending = true,
                    RequestedAt = DateTime.Now
                };
                _context.ProfileChangeRequests.Add(pendingRequest);
            }
            else
            {
                pendingRequest.PhotoPath = photoPath;
                pendingRequest.RequestedAt = DateTime.Now;
                _context.ProfileChangeRequests.Update(pendingRequest);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your profile photo change request has been submitted for Admin approval.";
            return RedirectToAction("Profile");
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> TeamCalendar()
        {
            ViewData["ActivePage"] = "Leaves";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.Id == empId);

            if (employee == null) return NotFound();

            return View(employee);
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> MyLeaves()
        {
            ViewData["ActivePage"] = "Leaves";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var leaves = await _context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == empId)
                .OrderByDescending(l => l.AppliedAt)
                .ToListAsync();

            return View(leaves);
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> ApplyLeave()
        {
            ViewData["ActivePage"] = "Leaves";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var leaveBalances = await _context.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.EmployeeId == empId)
                .ToListAsync();
            ViewBag.LeaveBalances = leaveBalances;

            var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            ViewData["LeaveTypeId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(leaveTypes, "Id", "Name");

            return View(new LeaveRequest { EmployeeId = empId });
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> MyPayroll()
        {
            ViewData["ActivePage"] = "Self"; // Using Self for payroll as per top-nav draft
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var payrolls = await _context.PayrollRecords
                .Where(p => p.EmployeeId == empId)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            return View(payrolls);
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave(LeaveRequest request)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            if (request.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
            }

            if (request.EndDate < request.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            // Overlap check
            var overlap = await _context.LeaveRequests
                .AnyAsync(l => l.EmployeeId == empId 
                    && l.Status != LeaveStatus.Rejected
                    && l.StartDate <= request.EndDate 
                    && l.EndDate >= request.StartDate);

            if (overlap)
            {
                ModelState.AddModelError("", "You already have a leave request that overlaps with these dates.");
            }

            // Holiday conflict check
            var holidayConflict = await _context.Holidays
                .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
                .FirstOrDefaultAsync();

            if (holidayConflict != null)
            {
                ModelState.AddModelError("", $"You cannot apply for leave on a registered public holiday: {holidayConflict.Name} ({holidayConflict.Date:dd MMM yyyy}).");
            }

            if (ModelState.IsValid)
            {
                request.EmployeeId = empId;
                request.AppliedAt = DateTime.Now;
                request.Status = LeaveStatus.Pending;
                
                _context.LeaveRequests.Add(request);
                await _context.SaveChangesAsync();

                // Notify Admin via Email
                var employee = await _context.Employees.FindAsync(empId);
                var admin = await _context.Users.Include(u => u.UserRoles!).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.UserRoles!.Any(ur => ur.Role!.Name == "Admin") && u.IsActive);
                
                if (admin != null && employee != null)
                {
                    var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
                    var typeName = leaveType?.Name ?? "Unknown";

                    string subject = $"New Leave Request: {employee.FullName}";
                    string body = $@"
                        <h3>New Leave Application</h3>
                        <p><strong>Employee:</strong> {employee.FullName} ({employee.EmployeeCode})</p>
                        <p><strong>Type:</strong> {typeName} {(request.HalfDayPeriod != HalfDayPeriod.None ? $"({request.HalfDayPeriod})" : "")}</p>
                        <p><strong>Dates:</strong> {request.StartDate:dd MMM yyyy} to {request.EndDate:dd MMM yyyy}</p>
                        <p><strong>Reason:</strong> {request.Reason}</p>
                        <p><a href='#'>Login to Approve/Reject</a></p>";
                    
                    // Using a default fallback email for POC since User model doesn't have an Email property
                    string adminEmail = "hr@company.com";
                    await _emailService.SendGenericEmailAsync(adminEmail, subject, body);
                }

                TempData["SuccessMessage"] = "Leave request submitted successfully. A notification has been sent to HR.";
                return RedirectToAction("MyLeaves");
            }

            var leaveBalances = await _context.LeaveBalances.Include(b => b.LeaveType).Where(lb => lb.EmployeeId == empId).ToListAsync();
            ViewBag.LeaveBalances = leaveBalances;

            var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            ViewData["LeaveTypeId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(leaveTypes, "Id", "Name", request.LeaveTypeId);

            return View(request);
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> ViewPaySlip(int id)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Unauthorized();

            var record = await _context.PayrollRecords
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == empId);

            if (record == null) return NotFound();

            return View("~/Views/Payroll/PaySlip.cshtml", record);
        }
    }
}
