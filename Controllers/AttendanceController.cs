using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Helpers;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class LocationClockModel
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? LocationName { get; set; }
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        public async Task<IActionResult> ClockIn([FromBody] LocationClockModel model)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Json(new { success = false, message = "Unauthorized user session." });

            var employee = await _context.Employees.FindAsync(empId);
            if (employee == null || !employee.IsActive) return Json(new { success = false, message = "Employee record not active." });

            var today = DateTime.Today;
            var todayRecord = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);

            if (todayRecord != null && todayRecord.ClockInTime.HasValue)
            {
                return Json(new { success = false, message = $"You have already clocked in today at {todayRecord.ClockInTime.Value:hh:mm tt}." });
            }

            // Location Verification Logic
            var activeLocations = await _context.OfficeLocations.Where(l => l.IsActive).ToListAsync();
            if (!activeLocations.Any())
            {
                // Seed fallback location if database has none
                var defaultLoc = new OfficeLocation
                {
                    Name = "Main Office Site",
                    Address = "HQ Campus",
                    Latitude = 23.0225,
                    Longitude = 72.5714,
                    AllowedRadiusMeters = 300.0,
                    IsActive = true,
                    IsEnforced = true
                };
                _context.OfficeLocations.Add(defaultLoc);
                await _context.SaveChangesAsync();
                activeLocations.Add(defaultLoc);
            }

            bool isLocationValid = false;
            OfficeLocation? matchedLocation = null;
            double shortestDistance = double.MaxValue;
            OfficeLocation? nearestLocation = null;

            foreach (var loc in activeLocations)
            {
                if (!loc.IsEnforced)
                {
                    isLocationValid = true;
                    matchedLocation = loc;
                    shortestDistance = 0;
                    break;
                }

                double dist = LocationHelper.CalculateDistanceInMeters(model.Latitude, model.Longitude, loc.Latitude, loc.Longitude);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    nearestLocation = loc;
                }

                if (dist <= loc.AllowedRadiusMeters)
                {
                    isLocationValid = true;
                    matchedLocation = loc;
                    break;
                }
            }

            if (!isLocationValid)
            {
                string locName = nearestLocation?.Name ?? "Office";
                double allowedRadius = nearestLocation?.AllowedRadiusMeters ?? 300;
                double distMeters = Math.Round(shortestDistance);
                return Json(new
                {
                    success = false,
                    isLocationError = true,
                    message = $"Location Verification Failed!\n\nYou are {distMeters} meters away from '{locName}' (Allowed perimeter: {allowedRadius}m).\n\nYou cannot Clock In outside the designated location."
                });
            }

            // Save Clock-In
            if (todayRecord == null)
            {
                todayRecord = new Attendance
                {
                    EmployeeId = empId,
                    Date = today,
                    ClockInTime = DateTime.Now,
                    ClockInLatitude = model.Latitude,
                    ClockInLongitude = model.Longitude,
                    ClockInLocationName = matchedLocation?.Name ?? model.LocationName ?? "Verified Location",
                    IsLocationVerified = true,
                    Status = "Clocked In",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Attendances.Add(todayRecord);
            }
            else
            {
                todayRecord.ClockInTime = DateTime.Now;
                todayRecord.ClockInLatitude = model.Latitude;
                todayRecord.ClockInLongitude = model.Longitude;
                todayRecord.ClockInLocationName = matchedLocation?.Name ?? model.LocationName ?? "Verified Location";
                todayRecord.IsLocationVerified = true;
                todayRecord.Status = "Clocked In";
                todayRecord.UpdatedAt = DateTime.Now;
                _context.Attendances.Update(todayRecord);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Clocked In successfully at {todayRecord.ClockInTime.Value:hh:mm tt} ({todayRecord.ClockInLocationName}).",
                clockInTime = todayRecord.ClockInTime.Value.ToString("hh:mm tt"),
                location = todayRecord.ClockInLocationName
            });
        }

        [AuthorizeRole("Employee")]
        [HttpPost]
        public async Task<IActionResult> ClockOut([FromBody] LocationClockModel model)
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Json(new { success = false, message = "Unauthorized user session." });

            var today = DateTime.Today;
            var todayRecord = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);

            if (todayRecord == null || !todayRecord.ClockInTime.HasValue)
            {
                return Json(new { success = false, message = "You have not clocked in today yet. Please Clock In first." });
            }

            if (todayRecord.ClockOutTime.HasValue)
            {
                return Json(new { success = false, message = $"You have already clocked out today at {todayRecord.ClockOutTime.Value:hh:mm tt}." });
            }

            // Location Verification Logic for Clock Out
            var activeLocations = await _context.OfficeLocations.Where(l => l.IsActive).ToListAsync();
            bool isLocationValid = false;
            OfficeLocation? matchedLocation = null;
            double shortestDistance = double.MaxValue;
            OfficeLocation? nearestLocation = null;

            foreach (var loc in activeLocations)
            {
                if (!loc.IsEnforced)
                {
                    isLocationValid = true;
                    matchedLocation = loc;
                    shortestDistance = 0;
                    break;
                }

                double dist = LocationHelper.CalculateDistanceInMeters(model.Latitude, model.Longitude, loc.Latitude, loc.Longitude);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    nearestLocation = loc;
                }

                if (dist <= loc.AllowedRadiusMeters)
                {
                    isLocationValid = true;
                    matchedLocation = loc;
                    break;
                }
            }

            if (!isLocationValid && activeLocations.Any())
            {
                string locName = nearestLocation?.Name ?? "Office";
                double allowedRadius = nearestLocation?.AllowedRadiusMeters ?? 300;
                double distMeters = Math.Round(shortestDistance);
                return Json(new
                {
                    success = false,
                    isLocationError = true,
                    message = $"Location Verification Failed!\n\nYou are {distMeters} meters away from '{locName}' (Allowed perimeter: {allowedRadius}m).\n\nYou cannot Clock Out outside the designated location."
                });
            }

            // Complete Clock Out
            todayRecord.ClockOutTime = DateTime.Now;
            todayRecord.ClockOutLatitude = model.Latitude;
            todayRecord.ClockOutLongitude = model.Longitude;
            todayRecord.ClockOutLocationName = matchedLocation?.Name ?? model.LocationName ?? "Verified Location";
            
            var duration = (todayRecord.ClockOutTime.Value - todayRecord.ClockInTime.Value).TotalMinutes;
            todayRecord.WorkDurationMinutes = Math.Max(0, duration);
            todayRecord.Status = "Completed";
            todayRecord.UpdatedAt = DateTime.Now;

            _context.Attendances.Update(todayRecord);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Clocked Out successfully at {todayRecord.ClockOutTime.Value:hh:mm tt}. Total work time: {todayRecord.DisplayWorkDuration}.",
                clockOutTime = todayRecord.ClockOutTime.Value.ToString("hh:mm tt"),
                duration = todayRecord.DisplayWorkDuration,
                location = todayRecord.ClockOutLocationName
            });
        }

        [AuthorizeRole("Employee")]
        [HttpGet]
        public async Task<IActionResult> GetTodayStatus()
        {
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return Json(new { isClockedIn = false, isClockedOut = false, error = "No employee session" });

            var today = DateTime.Today;
            var record = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);

            var activeOfficeLocations = await _context.OfficeLocations
                .Where(l => l.IsActive)
                .Select(l => new { l.Id, l.Name, l.Latitude, l.Longitude, l.AllowedRadiusMeters, l.IsEnforced })
                .ToListAsync();

            if (record == null)
            {
                return Json(new
                {
                    isClockedIn = false,
                    isClockedOut = false,
                    clockInTime = (string?)null,
                    clockOutTime = (string?)null,
                    workDuration = "--",
                    locationName = (string?)null,
                    officeLocations = activeOfficeLocations
                });
            }

            return Json(new
            {
                isClockedIn = record.ClockInTime.HasValue,
                isClockedOut = record.ClockOutTime.HasValue,
                clockInTime = record.ClockInTime?.ToString("hh:mm tt"),
                clockOutTime = record.ClockOutTime?.ToString("hh:mm tt"),
                workDuration = record.DisplayWorkDuration,
                locationName = record.ClockInLocationName ?? record.ClockOutLocationName,
                officeLocations = activeOfficeLocations
            });
        }

        [AuthorizeRole("Employee")]
        public async Task<IActionResult> MyAttendance()
        {
            ViewData["ActivePage"] = "Attendance";
            var empIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!int.TryParse(empIdStr, out int empId)) return RedirectToAction("Login", "Auth");

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == empId)
                .OrderByDescending(a => a.Date)
                .Take(60)
                .ToListAsync();

            ViewBag.OfficeLocations = await _context.OfficeLocations.Where(l => l.IsActive).ToListAsync();
            return View(attendances);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var roles = HttpContext.Session.GetString("UserRoles") ?? "";
            if (roles.Contains("Admin") || roles.Contains("Manager") || roles.Contains("HR"))
            {
                return RedirectToAction(nameof(AdminIndex));
            }
            return RedirectToAction(nameof(MyAttendance));
        }

        // --- ADMIN CONTROLLER ACTIONS ---

        [AuthorizeRole("Admin")]
        public async Task<IActionResult> AdminIndex(DateTime? startDate, DateTime? endDate, int? departmentId)
        {
            ViewData["ActivePage"] = "Admin";
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
                .Where(a => a.Date >= start && a.Date <= end);

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(a => a.Employee != null && a.Employee.DepartmentId == departmentId.Value);
            }

            var records = await query.OrderByDescending(a => a.Date).ThenBy(a => a.Employee!.FirstName).ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            ViewBag.DepartmentId = departmentId;

            return View(records);
        }

        [AuthorizeRole("Admin")]
        public async Task<IActionResult> ManageLocations()
        {
            ViewData["ActivePage"] = "Admin";
            var locations = await _context.OfficeLocations.OrderByDescending(l => l.CreatedAt).ToListAsync();
            return View(locations);
        }

        [AuthorizeRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLocation(OfficeLocation location)
        {
            if (ModelState.IsValid)
            {
                if (location.Id == 0)
                {
                    location.CreatedAt = DateTime.Now;
                    location.UpdatedAt = DateTime.Now;
                    _context.OfficeLocations.Add(location);
                    TempData["SuccessMessage"] = $"Office Location '{location.Name}' added successfully.";
                }
                else
                {
                    var existing = await _context.OfficeLocations.FindAsync(location.Id);
                    if (existing != null)
                    {
                        existing.Name = location.Name;
                        existing.Address = location.Address;
                        existing.Latitude = location.Latitude;
                        existing.Longitude = location.Longitude;
                        existing.AllowedRadiusMeters = location.AllowedRadiusMeters;
                        existing.IsActive = location.IsActive;
                        existing.IsEnforced = location.IsEnforced;
                        existing.UpdatedAt = DateTime.Now;
                        _context.OfficeLocations.Update(existing);
                        TempData["SuccessMessage"] = $"Office Location '{location.Name}' updated successfully.";
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageLocations));
            }
            var locations = await _context.OfficeLocations.ToListAsync();
            return View(nameof(ManageLocations), locations);
        }

        [AuthorizeRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.OfficeLocations.FindAsync(id);
            if (location != null)
            {
                _context.OfficeLocations.Remove(location);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Office Location deleted successfully.";
            }
            return RedirectToAction(nameof(ManageLocations));
        }
    }
}
