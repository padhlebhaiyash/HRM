using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class LeaveBalanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.LeaveBalances)
                .ThenInclude(b => b.LeaveType)
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ToListAsync();

            ViewBag.LeaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();

            return View(employees);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Allocate(int employeeId, Dictionary<string, decimal> allocations)
        {
            if (allocations == null || !allocations.Any())
            {
                TempData["ErrorMessage"] = "No allocations provided.";
                return RedirectToAction(nameof(Index));
            }

            // Validate all allocations first to ensure all are valid
            foreach (var kvp in allocations)
            {
                if (kvp.Value < 0)
                {
                    TempData["ErrorMessage"] = $"Allocated amount for {kvp.Key} cannot be negative.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var employee = await _context.Employees
                .Include(e => e.LeaveBalances)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            var leaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive).ToDictionaryAsync(lt => lt.Code.ToLower());

            int updatedCount = 0;
            foreach (var kvp in allocations)
            {
                var codeKey = kvp.Key.ToLower();
                if (leaveTypes.TryGetValue(codeKey, out var leaveType))
                {
                    var allocated = kvp.Value;
                    var balance = employee.LeaveBalances?.FirstOrDefault(b => b.LeaveTypeId == leaveType.Id);

                    if (balance == null)
                    {
                        balance = new LeaveBalance
                        {
                            EmployeeId = employeeId,
                            LeaveTypeId = leaveType.Id,
                            Allocated = allocated,
                            Used = 0
                        };
                        _context.LeaveBalances.Add(balance);
                    }
                    else
                    {
                        balance.Allocated = allocated;
                        _context.LeaveBalances.Update(balance);
                    }
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Leave allocations updated successfully for {employee.FullName}.";
            }
            else
            {
                TempData["ErrorMessage"] = "No valid leave types to update.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Download a pre-filled CSV with leave types as columns (one row per employee)
        [HttpGet]
        public async Task<IActionResult> DownloadTemplate()
        {
            var employees = await _context.Employees
                .Include(e => e.LeaveBalances)
                .ThenInclude(b => b.LeaveType)
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName)
                .ToListAsync();

            var leaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            var sb = new StringBuilder();

            // Header row: EmployeeCode, EmployeeName, then one column per leave type
            sb.Append("EmployeeCode,EmployeeName");
            foreach (var lt in leaveTypes)
                sb.Append($",{lt.Code}");
            sb.AppendLine();

            // One data row per employee
            foreach (var emp in employees)
            {
                var name = emp.FullName.Contains(',') ? $"\"{emp.FullName}\"" : emp.FullName;
                sb.Append($"{emp.EmployeeCode},{name}");
                foreach (var lt in leaveTypes)
                {
                    var allocated = emp.LeaveBalances?.FirstOrDefault(b => b.LeaveTypeId == lt.Id)?.Allocated ?? 0;
                    sb.Append($",{allocated}");
                }
                sb.AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"LeaveAllocation_{DateTime.Today:yyyyMMdd}.csv");
        }

        // POST: Import allocations from uploaded CSV (wide format — leave types as columns)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a CSV file to upload.";
                return RedirectToAction(nameof(Index));
            }

            if (!Path.GetExtension(csvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only .csv files are supported.";
                return RedirectToAction(nameof(Index));
            }

            // Load all active employees keyed by code
            var employees = await _context.Employees
                .Include(e => e.LeaveBalances)
                .Where(e => e.IsActive)
                .ToDictionaryAsync(e => e.EmployeeCode.Trim().ToUpper());

            // Load all active leave types
            var dbLeaveTypes = await _context.LeaveTypes.Where(t => t.IsActive).ToDictionaryAsync(t => t.Code.Trim().ToLower());

            var errors = new List<string>();
            int updated = 0, created = 0;
            int lineNum = 0;

            // Map: column index → LeaveType (populated after reading header)
            var columnLeaveTypes = new Dictionary<int, LeaveType>();

            using var reader = new StreamReader(csvFile.OpenReadStream());
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineNum++;

                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = SplitCsvLine(line);

                // First non-empty line is the header — parse column positions
                if (columnLeaveTypes.Count == 0)
                {
                    for (int i = 2; i < parts.Length; i++)
                    {
                        var header = parts[i].Trim().ToLower();
                        if (dbLeaveTypes.TryGetValue(header, out var lt))
                            columnLeaveTypes[i] = lt;
                        else
                            errors.Add($"Header column {i + 1} '{parts[i].Trim()}' is not a recognised Leave Type and will be ignored.");
                    }
                    continue; // done with header
                }

                if (parts.Length < 2)
                {
                    errors.Add($"Row {lineNum}: Not enough columns.");
                    continue;
                }

                var code = parts[0].Trim().ToUpper();
                if (!employees.TryGetValue(code, out var emp))
                {
                    errors.Add($"Row {lineNum}: Employee code '{code}' not found or inactive.");
                    continue;
                }

                // Process each leave type column
                foreach (var (colIdx, leaveType) in columnLeaveTypes)
                {
                    if (colIdx >= parts.Length)
                    {
                        errors.Add($"Row {lineNum} ({code}): Missing value for {leaveType.Code}.");
                        continue;
                    }

                    var allocatedStr = parts[colIdx].Trim();
                    if (!decimal.TryParse(allocatedStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var allocated) || allocated < 0)
                    {
                        errors.Add($"Row {lineNum} ({code}): Invalid value '{allocatedStr}' for {leaveType.Code}. Must be ≥ 0.");
                        continue;
                    }

                    var balance = emp.LeaveBalances?.FirstOrDefault(b => b.LeaveTypeId == leaveType.Id);
                    if (balance == null)
                    {
                        _context.LeaveBalances.Add(new LeaveBalance
                        {
                            EmployeeId = emp.Id,
                            LeaveTypeId = leaveType.Id,
                            Allocated = allocated,
                            Used = 0
                        });
                        created++;
                    }
                    else
                    {
                        balance.Allocated = allocated;
                        _context.LeaveBalances.Update(balance);
                        updated++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var summary = $"Import complete: {created} created, {updated} updated.";
            TempData[errors.Any() ? "ErrorMessage" : "SuccessMessage"] =
                errors.Any() ? $"{summary} {errors.Count} issue(s):<br>" + string.Join("<br>", errors) : summary;

            return RedirectToAction(nameof(Index));
        }

        // Simple CSV line splitter that handles quoted fields (e.g. "Smith, John")
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuote = false;
            var current = new System.Text.StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuote = !inQuote; }
                else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
                else { current.Append(c); }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
