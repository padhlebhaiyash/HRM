using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class LeaveTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var leaveTypes = await _context.LeaveTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(leaveTypes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveType leaveType)
        {
            if (ModelState.IsValid)
            {
                if (await _context.LeaveTypes.AnyAsync(t => t.Name.ToLower() == leaveType.Name.ToLower() || t.Code.ToLower() == leaveType.Code.ToLower()))
                {
                    TempData["ErrorMessage"] = "A leave type with this name or code already exists.";
                    return RedirectToAction(nameof(Index));
                }

                _context.LeaveTypes.Add(leaveType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Leave type '{leaveType.Name}' created successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid data. Please check fields.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null) return NotFound();

            // Default system leave types should not be deactivated to prevent core logic breakages
            var coreCodes = new[] { "Casual", "Sick", "Earned", "Unpaid", "WFH" };
            if (coreCodes.Contains(leaveType.Code))
            {
                TempData["ErrorMessage"] = "Core system leave types cannot be deactivated.";
                return RedirectToAction(nameof(Index));
            }

            leaveType.IsActive = !leaveType.IsActive;
            _context.LeaveTypes.Update(leaveType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Leave type status updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv()
        {
            var items = await _context.LeaveTypes.ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Code,IsActive");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.Name)},{Helpers.CsvHelper.Escape(item.Code)},{item.IsActive}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "leave_types.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromCsv(Microsoft.AspNetCore.Http.IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid CSV file.";
                return RedirectToAction(nameof(Index));
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
                        var code = parts[1].Trim();
                        var isActive = parts.Count >= 3 ? (bool.TryParse(parts[2], out var act) ? act : true) : true;

                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code)) continue;

                        var existing = await _context.LeaveTypes.FirstOrDefaultAsync(l => l.Code.ToLower() == code.ToLower());
                        if (existing == null)
                        {
                            var lt = new LeaveType
                            {
                                Name = name,
                                Code = code,
                                IsActive = isActive
                            };
                            _context.LeaveTypes.Add(lt);
                        }
                        else
                        {
                            existing.Name = name;
                            existing.IsActive = isActive;
                            _context.LeaveTypes.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Leave Types imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
