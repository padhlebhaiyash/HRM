using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments.ToListAsync();
            return View(departments);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                department.CreatedAt = DateTime.Now;
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Department created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Department updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null) return NotFound();

            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                // Soft delete or hard delete? Let's check if there are employees.
                var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
                if (hasEmployees)
                {
                    TempData["ErrorMessage"] = "Cannot delete department as it has active employees. Please reassign them first.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Department deleted successfully.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv()
        {
            var items = await _context.Departments.ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Description,IsActive");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.Name)},{Helpers.CsvHelper.Escape(item.Description)},{item.IsActive}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "departments.csv");
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
                        var description = parts[1].Trim();
                        var isActive = parts.Count >= 3 ? (bool.TryParse(parts[2], out var act) ? act : true) : true;

                        if (string.IsNullOrEmpty(name)) continue;

                        var existing = await _context.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());
                        if (existing == null)
                        {
                            var dept = new Department
                            {
                                Name = name,
                                Description = description,
                                IsActive = isActive,
                                CreatedAt = DateTime.Now
                            };
                            _context.Departments.Add(dept);
                        }
                        else
                        {
                            existing.Description = description;
                            existing.IsActive = isActive;
                            _context.Departments.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Departments imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
