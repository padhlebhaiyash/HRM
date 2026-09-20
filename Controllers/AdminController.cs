using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var totalDepartments = await _context.Departments.CountAsync(d => d.IsActive);
            var pendingLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);
            var pendingProfileRequests = await _context.ProfileChangeRequests.CountAsync(r => r.IsPending);
            var totalDesignations = await _context.Designations.CountAsync();
            
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.PendingProfileRequests = pendingProfileRequests;
            ViewBag.TotalDesignations = totalDesignations;

            return View();
        }

        public async Task<IActionResult> ProfileRequests()
        {
            var requests = await _context.ProfileChangeRequests
                .Include(r => r.Employee)
                .Where(r => r.IsPending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfileRequest(int id)
        {
            var request = await _context.ProfileChangeRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsPending);

            if (request == null) return NotFound();

            if (request.Employee != null)
            {
                if (request.LocalAddress != null) request.Employee.LocalAddress = request.LocalAddress;
                if (request.PermanentAddress != null) request.Employee.PermanentAddress = request.PermanentAddress;
                if (request.PhotoPath != null) request.Employee.PhotoPath = request.PhotoPath;
                if (request.FathersName != null) request.Employee.FathersName = request.FathersName;
                if (request.DateOfBirth.HasValue) request.Employee.DateOfBirth = request.DateOfBirth;
                if (request.Gender.HasValue) request.Employee.Gender = request.Gender.Value;
                if (request.Phone != null) request.Employee.Phone = request.Phone;
                request.Employee.UpdatedAt = DateTime.Now;
                _context.Employees.Update(request.Employee);
            }

            request.IsPending = false;
            request.IsApproved = true;
            request.ReviewedAt = DateTime.Now;
            _context.ProfileChangeRequests.Update(request);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile change request approved successfully.";
            return RedirectToAction(nameof(ProfileRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProfileRequest(int id)
        {
            var request = await _context.ProfileChangeRequests
                .FirstOrDefaultAsync(r => r.Id == id && r.IsPending);

            if (request == null) return NotFound();

            request.IsPending = false;
            request.IsApproved = false;
            request.ReviewedAt = DateTime.Now;
            _context.ProfileChangeRequests.Update(request);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile change request rejected.";
            return RedirectToAction(nameof(ProfileRequests));
        }

        [HttpGet]
        public async Task<IActionResult> CustomizePortal()
        {
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();
            if (companySetting == null)
            {
                companySetting = new CompanySetting 
                { 
                    CompanyName = null,
                    AppBaseUrl = "https://munrohr.inovexa.solutions",
                    FromEmail = "techinovexasolutions@gmail.com",
                    FromName = "HRM System"
                };
                _context.CompanySettings.Add(companySetting);
                await _context.SaveChangesAsync();
            }

            ViewBag.Notices = await _context.Notices.OrderByDescending(n => n.Date).ToListAsync();
            ViewBag.Holidays = await _context.Holidays.OrderBy(h => h.Date).ToListAsync();

            return View(companySetting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompanySettings(CompanySetting settings, Microsoft.AspNetCore.Http.IFormFile? logoFile, bool clearLogoImage = false, bool clearLogoText = false)
        {
            var existing = await _context.CompanySettings.FirstOrDefaultAsync(s => s.Id == settings.Id);
            if (existing != null)
            {
                if (clearLogoText)
                {
                    existing.CompanyName = null;
                }
                else
                {
                    existing.CompanyName = settings.CompanyName;
                }

                existing.AppBaseUrl = string.IsNullOrWhiteSpace(settings.AppBaseUrl) ? null : settings.AppBaseUrl.Trim().TrimEnd('/');
                existing.FromEmail = string.IsNullOrWhiteSpace(settings.FromEmail) ? null : settings.FromEmail.Trim();
                existing.FromName = string.IsNullOrWhiteSpace(settings.FromName) ? null : settings.FromName.Trim();

                if (clearLogoImage)
                {
                    existing.LogoImagePath = null;
                }
                else if (logoFile != null && logoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logos");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoFile.CopyToAsync(fileStream);
                    }
                    existing.LogoImagePath = "/img/logos/" + uniqueFileName;
                }

                _context.CompanySettings.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Portal branding and email configurations updated successfully.";
            }
            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotice(Notice notice)
        {
            if (ModelState.IsValid)
            {
                _context.Notices.Add(notice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "New notice board entry published.";
            }
            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice != null)
            {
                _context.Notices.Remove(notice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notice entry deleted.";
            }
            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHoliday(Holiday holiday)
        {
            if (ModelState.IsValid)
            {
                _context.Holidays.Add(holiday);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Holiday added to the system.";
            }
            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday != null)
            {
                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Holiday deleted.";
            }
            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpGet]
        public async Task<IActionResult> ExportNoticesToCsv()
        {
            var items = await _context.Notices.ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Title,Content,Date,IsActive");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.Title)},{Helpers.CsvHelper.Escape(item.Content)},{item.Date.ToString("yyyy-MM-dd")},{item.IsActive}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "notice_board.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportNoticesFromCsv(Microsoft.AspNetCore.Http.IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid CSV file.";
                return RedirectToAction(nameof(CustomizePortal));
            }

            try
            {
                using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
                {
                    var header = await reader.ReadLineAsync(); // skip header
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = Helpers.CsvHelper.ParseCsvLine(line);
                        if (parts.Count < 2) continue;

                        var title = parts[0].Trim();
                        var content = parts[1].Trim();
                        
                        DateTime date = DateTime.Now;
                        if (parts.Count >= 3 && DateTime.TryParse(parts[2].Trim(), out var pDate)) date = pDate;

                        bool isActive = true;
                        if (parts.Count >= 4 && bool.TryParse(parts[3].Trim(), out var pActive)) isActive = pActive;

                        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content)) continue;

                        var existing = await _context.Notices.FirstOrDefaultAsync(n => n.Title.ToLower() == title.ToLower());
                        if (existing == null)
                        {
                            var notice = new Notice
                            {
                                Title = title,
                                Content = content,
                                Date = date,
                                IsActive = isActive
                            };
                            _context.Notices.Add(notice);
                        }
                        else
                        {
                            existing.Content = content;
                            existing.Date = date;
                            existing.IsActive = isActive;
                            _context.Notices.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Notice board entries imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import notices: {ex.Message}";
            }

            return RedirectToAction(nameof(CustomizePortal));
        }

        [HttpGet]
        public async Task<IActionResult> ExportHolidaysToCsv()
        {
            var items = await _context.Holidays.ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Date,Color");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.Name)},{item.Date.ToString("yyyy-MM-dd")},{Helpers.CsvHelper.Escape(item.Color)}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "holidays.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportHolidaysFromCsv(Microsoft.AspNetCore.Http.IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid CSV file.";
                return RedirectToAction(nameof(CustomizePortal));
            }

            try
            {
                using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
                {
                    var header = await reader.ReadLineAsync(); // skip header
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = Helpers.CsvHelper.ParseCsvLine(line);
                        if (parts.Count < 2) continue;

                        var name = parts[0].Trim();
                        
                        DateTime date = DateTime.Now;
                        if (!DateTime.TryParse(parts[1].Trim(), out date)) continue;

                        var color = parts.Count >= 3 ? parts[2].Trim() : "primary";

                        if (string.IsNullOrEmpty(name)) continue;

                        var existing = await _context.Holidays.FirstOrDefaultAsync(h => h.Name.ToLower() == name.ToLower() && h.Date == date);
                        if (existing == null)
                        {
                            var holiday = new Holiday
                            {
                                Name = name,
                                Date = date,
                                Color = color
                            };
                            _context.Holidays.Add(holiday);
                        }
                        else
                        {
                            existing.Color = color;
                            _context.Holidays.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Holidays list imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import holidays: {ex.Message}";
            }

            return RedirectToAction(nameof(CustomizePortal));
        }
    }
}
