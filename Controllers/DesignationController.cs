using System.Linq;
using System.Threading.Tasks;
using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class DesignationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var designations = await _context.Designations
                .Include(d => d.ParentDesignation)
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .ToListAsync();

            ViewBag.ParentDesignations = designations;
            return View(designations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Designation designation)
        {
            // Set ParentDesignationId to null if it's 0 (meaning "None / System Root" selected in dropdown)
            if (designation.ParentDesignationId == 0)
            {
                designation.ParentDesignationId = null;
            }

            if (ModelState.IsValid)
            {
                if (await _context.Designations.AnyAsync(d => d.Name.ToLower() == designation.Name.ToLower()))
                {
                    TempData["ErrorMessage"] = "A designation with this name already exists.";
                    return RedirectToAction(nameof(Index));
                }

                if (designation.ParentDesignationId == null)
                {
                    var existingRoot = await _context.Designations.AnyAsync(d => d.ParentDesignationId == null);
                    if (existingRoot)
                    {
                        TempData["ErrorMessage"] = "Only one head/root designation is allowed in the system.";
                        return RedirectToAction(nameof(Index));
                    }
                    designation.Level = 1;
                }
                else
                {
                    var parent = await _context.Designations.FindAsync(designation.ParentDesignationId.Value);
                    if (parent == null)
                    {
                        TempData["ErrorMessage"] = "The selected parent designation does not exist.";
                        return RedirectToAction(nameof(Index));
                    }
                    designation.Level = parent.Level + 1;
                }

                _context.Designations.Add(designation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Designation '{designation.Name}' created successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid data. Please check all fields.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return NotFound();

            var descendantIds = new List<int>();
            await GetDescendantsAsync(designation.Id, descendantIds);

            var eligibleParents = await _context.Designations
                .Where(d => d.Id != designation.Id && !descendantIds.Contains(d.Id))
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .ToListAsync();

            ViewBag.ParentDesignations = eligibleParents;
            return View(designation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Designation designation)
        {
            if (id != designation.Id) return NotFound();

            if (designation.ParentDesignationId == 0)
            {
                designation.ParentDesignationId = null;
            }

            if (ModelState.IsValid)
            {
                if (await _context.Designations.AnyAsync(d => d.Name.ToLower() == designation.Name.ToLower() && d.Id != id))
                {
                    ModelState.AddModelError("Name", "A designation with this name already exists.");
                }

                if (designation.ParentDesignationId == null)
                {
                    var existingRoot = await _context.Designations.FirstOrDefaultAsync(d => d.ParentDesignationId == null && d.Id != id);
                    if (existingRoot != null)
                    {
                        ModelState.AddModelError("ParentDesignationId", "Only one head/root designation is allowed.");
                    }
                }
                else
                {
                    var descendantIds = new List<int>();
                    await GetDescendantsAsync(id, descendantIds);
                    if (descendantIds.Contains(designation.ParentDesignationId.Value) || designation.ParentDesignationId.Value == id)
                    {
                        ModelState.AddModelError("ParentDesignationId", "Selecting this parent would cause a circular reference.");
                    }
                }

                if (ModelState.IsValid)
                {
                    int newLevel = 1;
                    if (designation.ParentDesignationId != null)
                    {
                        var parent = await _context.Designations.FindAsync(designation.ParentDesignationId.Value);
                        if (parent != null)
                        {
                            newLevel = parent.Level + 1;
                        }
                    }

                    // Track old level before updating
                    var existingEntity = await _context.Designations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
                    int oldLevel = existingEntity?.Level ?? designation.Level;
                    designation.Level = newLevel;

                    _context.Designations.Update(designation);
                    await _context.SaveChangesAsync();

                    if (oldLevel != newLevel)
                    {
                        await UpdateLevelsRecursivelyAsync(designation.Id, newLevel);
                    }

                    TempData["SuccessMessage"] = $"Designation '{designation.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var allDescendantIds = new List<int>();
            await GetDescendantsAsync(id, allDescendantIds);
            ViewBag.ParentDesignations = await _context.Designations
                .Where(d => d.Id != id && !allDescendantIds.Contains(d.Id))
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .ToListAsync();

            return View(designation);
        }

        private async Task GetDescendantsAsync(int designationId, List<int> descendantIds)
        {
            var children = await _context.Designations
                .Where(d => d.ParentDesignationId == designationId)
                .Select(d => d.Id)
                .ToListAsync();

            foreach (var childId in children)
            {
                descendantIds.Add(childId);
                await GetDescendantsAsync(childId, descendantIds);
            }
        }

        private async Task UpdateLevelsRecursivelyAsync(int designationId, int parentLevel)
        {
            var children = await _context.Designations
                .Where(d => d.ParentDesignationId == designationId)
                .ToListAsync();

            foreach (var child in children)
            {
                int oldLevel = child.Level;
                int newLevel = parentLevel + 1;

                if (oldLevel != newLevel)
                {
                    child.Level = newLevel;
                    _context.Designations.Update(child);
                    await _context.SaveChangesAsync();
                    await UpdateLevelsRecursivelyAsync(child.Id, newLevel);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv()
        {
            var items = await _context.Designations.Include(d => d.ParentDesignation).ToListAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,ParentDesignationName");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.Name)},{Helpers.CsvHelper.Escape(item.ParentDesignation?.Name)}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "designations.csv");
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
                var root = await _context.Designations.FirstOrDefaultAsync(d => d.ParentDesignationId == null);

                using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
                {
                    var header = await reader.ReadLineAsync(); // skip header
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = Helpers.CsvHelper.ParseCsvLine(line);
                        if (parts.Count < 1) continue;

                        var name = parts[0].Trim();
                        var parentName = parts.Count >= 2 ? parts[1].Trim() : string.Empty;

                        if (string.IsNullOrEmpty(name)) continue;

                        // Check if parent designation exists
                        int? parentId = null;
                        int level = 1;

                        if (!string.IsNullOrEmpty(parentName))
                        {
                            var parent = await _context.Designations.FirstOrDefaultAsync(d => d.Name.ToLower() == parentName.ToLower());
                            if (parent != null)
                            {
                                parentId = parent.Id;
                                level = parent.Level + 1;
                            }
                            else if (root != null)
                            {
                                // Fallback to existing root
                                parentId = root.Id;
                                level = root.Level + 1;
                            }
                        }
                        else
                        {
                            // If no parent name is provided, check if we already have a root
                            if (root != null)
                            {
                                // Attach to the existing root so it's not another root
                                parentId = root.Id;
                                level = root.Level + 1;
                            }
                            else
                            {
                                // This will be the root
                                parentId = null;
                                level = 1;
                            }
                        }

                        var existing = await _context.Designations.FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());
                        if (existing == null)
                        {
                            var desig = new Designation
                            {
                                Name = name,
                                ParentDesignationId = parentId,
                                Level = level
                            };
                            _context.Designations.Add(desig);
                            await _context.SaveChangesAsync(); // save to get designation ID
                            
                            // If we didn't have a root before, this is now the root
                            if (parentId == null && root == null)
                            {
                                root = desig;
                            }
                        }
                        else
                        {
                            // Avoid setting parent to self
                            if (existing.Id != parentId)
                            {
                                existing.ParentDesignationId = parentId;
                                existing.Level = level;
                                _context.Designations.Update(existing);
                                await _context.SaveChangesAsync();
                                await UpdateLevelsRecursivelyAsync(existing.Id, level);
                            }
                        }
                    }
                }
                TempData["SuccessMessage"] = "Designations imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
