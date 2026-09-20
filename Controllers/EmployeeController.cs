using HRMSystem.Data;
using HRMSystem.Filters;
using HRMSystem.Models;
using HRMSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Controllers
{
    [AuthorizeRole("Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public EmployeeController(ApplicationDbContext context, AuthService authService, EmailService emailService, IConfiguration config)
        {
            _context = context;
            _authService = authService;
            _emailService = emailService;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.User)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["DepartmentId"] = new SelectList(await _context.Departments.Where(d => d.IsActive).ToListAsync(), "Id", "Name");
            ViewData["DesignationId"] = new SelectList(await _context.Designations.OrderBy(d => d.Level).ToListAsync(), "Id", "Name");
            
            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.AllDesignations = await _context.Designations.OrderBy(d => d.Level).ThenBy(d => d.Name).ToListAsync();
            
            // Initially load Admin employee(s) as eligible managers
            var initialManagers = await _context.Employees
                .Include(e => e.Designation)
                .Where(e => e.IsActive && e.Designation != null && e.Designation.Name == "Admin")
                .ToListAsync();
            ViewData["ManagerId"] = new SelectList(initialManagers, "Id", "FullNameWithDesignation");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string username, List<int> selectedRoles)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("username", "Username is required for the employee account.");
            }
            else if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("username", "Username is already taken.");
            }

            if (!string.IsNullOrEmpty(employee.EmployeeCode) && await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode))
            {
                ModelState.AddModelError("EmployeeCode", "An employee with this Employee Code already exists.");
            }

            if (!string.IsNullOrEmpty(employee.Email))
            {
                var adminDesignation = await _context.Designations.FirstOrDefaultAsync(d => d.Name == "Admin");
                int adminDesigId = adminDesignation?.Id ?? 0;

                var conflictingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == employee.Email.ToLower());

                if (conflictingEmployee != null)
                {
                    bool isCurrentAdmin = employee.DesignationId == adminDesigId;
                    bool isConflictingAdmin = conflictingEmployee.DesignationId == adminDesigId;

                    if (!isCurrentAdmin && !isConflictingAdmin)
                    {
                        ModelState.AddModelError("Email", "An employee with this Email address already exists.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                employee.CreatedAt = DateTime.Now;
                employee.UpdatedAt = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync(); // Save to get the generated Id

                // Create User login account for the new employee, mapping selected roles dynamically
                var result = await _authService.CreateEmployeeAccountAsync(employee.Id, username, selectedRoles);

                // Generate setup password reset token and send email
                var resetTokenResult = await _authService.GeneratePasswordResetTokenAsync(employee.Email);
                string emailNotice = "";
                if (resetTokenResult != null)
                {
                    var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();
                    var baseUrl = companySetting?.AppBaseUrl?.TrimEnd('/');

                    string? resetLink;
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        resetLink = $"{baseUrl}/Auth/ResetPasswordViaToken?token={System.Net.WebUtility.UrlEncode(resetTokenResult.Value.token)}";
                    }
                    else
                    {
                        resetLink = Url.Action("ResetPasswordViaToken", "Auth", 
                            new { token = resetTokenResult.Value.token }, protocol: Request.Scheme);
                    }

                    int expiryMinutes = _config.GetValue<int>("EmailSettings:TokenExpiryMinutes", 30);
                    
                    try
                    {
                        await _emailService.SendWelcomeAndSetupEmailAsync(
                            employee.Email, 
                            employee.FullName, 
                            username,
                            resetLink!, 
                            expiryMinutes);
                        emailNotice = " A welcome email with password setup instructions has been sent to their inbox.";
                    }
                    catch (Exception ex)
                    {
                        emailNotice = $" <span class='text-danger'>(Warning: Welcome email could not be sent: {ex.Message})</span>";
                    }
                }

                TempData["SuccessMessage"] = $"Employee created successfully. Login account created.{emailNotice}<br><b>Username:</b> {username}<br><b>Password:</b> {result.rawPassword}";
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive), "Id", "Name", employee.DepartmentId);
            ViewData["DesignationId"] = new SelectList(await _context.Designations.OrderBy(d => d.Level).ToListAsync(), "Id", "Name", employee.DesignationId);
            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.AllDesignations = await _context.Designations.OrderBy(d => d.Level).ThenBy(d => d.Name).ToListAsync();

            var selectedDesignation = await _context.Designations.FindAsync(employee.DesignationId);
            var selectedLevel = selectedDesignation?.Level ?? 999;
            var managers = await _context.Employees
                .Include(e => e.Designation)
                .Where(e => e.IsActive && e.Designation != null && e.Designation.Level < selectedLevel)
                .ToListAsync();
            if (!managers.Any())
            {
                managers = await _context.Employees
                    .Include(e => e.Designation)
                    .Where(e => e.IsActive && e.Designation != null && e.Designation.Name == "Admin")
                    .ToListAsync();
            }
            ViewData["ManagerId"] = new SelectList(managers, "Id", "FullNameWithDesignation", employee.ManagerId);
            return View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Designation)
                .Include(e => e.BankDetail)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();

            if (employee.BankDetail == null)
            {
                employee.BankDetail = new BankDetail { EmployeeId = employee.Id };
            }

            var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
            ViewBag.SelectedRoles = user?.UserRoles?.Select(ur => ur.RoleId).ToList() ?? new List<int>();

            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive), "Id", "Name", employee.DepartmentId);
            ViewData["DesignationId"] = new SelectList(await _context.Designations.OrderBy(d => d.Level).ToListAsync(), "Id", "Name", employee.DesignationId);
            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.AllDesignations = await _context.Designations.OrderBy(d => d.Level).ThenBy(d => d.Name).ToListAsync();

            var selectedLevel = employee.Designation?.Level ?? 999;
            var managers = await _context.Employees
                .Include(e => e.Designation)
                .Where(e => e.IsActive && e.Designation != null && e.Designation.Level < selectedLevel && e.Id != employee.Id)
                .ToListAsync();
            if (!managers.Any())
            {
                managers = await _context.Employees
                    .Include(e => e.Designation)
                    .Where(e => e.IsActive && e.Designation != null && e.Designation.Name == "Admin" && e.Id != employee.Id)
                    .ToListAsync();
            }
            ViewData["ManagerId"] = new SelectList(managers, "Id", "FullNameWithDesignation", employee.ManagerId);
            
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, List<int> selectedRoles)
        {
            if (id != employee.Id) return NotFound();

            if (!string.IsNullOrEmpty(employee.Email))
            {
                var adminDesignation = await _context.Designations.FirstOrDefaultAsync(d => d.Name == "Admin");
                int adminDesigId = adminDesignation?.Id ?? 0;

                var conflictingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == employee.Email.ToLower() && e.Id != employee.Id);

                if (conflictingEmployee != null)
                {
                    bool isCurrentAdmin = employee.DesignationId == adminDesigId;
                    bool isConflictingAdmin = conflictingEmployee.DesignationId == adminDesigId;

                    if (!isCurrentAdmin && !isConflictingAdmin)
                    {
                        ModelState.AddModelError("Email", "An employee with this Email address already exists.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Employees.FindAsync(employee.Id);
                    if (existing == null) return NotFound();

                    existing.FirstName = employee.FirstName;
                    existing.LastName = employee.LastName;
                    existing.Email = employee.Email;
                    existing.DepartmentId = employee.DepartmentId;
                    existing.DesignationId = employee.DesignationId;
                    existing.ManagerId = employee.ManagerId;
                    existing.Salary = employee.Salary;
                    existing.DateOfJoining = employee.DateOfJoining;
                    existing.IsActive = employee.IsActive;
                    existing.UpdatedAt = DateTime.Now;

                    _context.Employees.Update(existing);

                    // Update Bank Details
                    var bankDetail = await _context.BankDetails.FirstOrDefaultAsync(b => b.EmployeeId == employee.Id);
                    if (bankDetail == null)
                    {
                        bankDetail = new BankDetail { EmployeeId = employee.Id };
                        _context.BankDetails.Add(bankDetail);
                    }
                    if (employee.BankDetail != null)
                    {
                        bankDetail.AccountHolderName = employee.BankDetail.AccountHolderName;
                        bankDetail.AccountNumber = employee.BankDetail.AccountNumber;
                        bankDetail.BankName = employee.BankDetail.BankName;
                        bankDetail.BSB = employee.BankDetail.BSB;
                        bankDetail.TaxPayerId = employee.BankDetail.TaxPayerId;
                    }

                    // Update Roles
                    var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
                    if (user != null && selectedRoles != null && selectedRoles.Any())
                    {
                        var existingMap = _context.UserRoles.Where(ur => ur.UserId == user.Id);
                        _context.UserRoles.RemoveRange(existingMap);
                        foreach(var rId in selectedRoles)
                        {
                            _context.UserRoles.Add(new UserRoleMapping { UserId = user.Id, RoleId = rId });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Employee updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive), "Id", "Name", employee.DepartmentId);
            ViewData["DesignationId"] = new SelectList(await _context.Designations.OrderBy(d => d.Level).ToListAsync(), "Id", "Name", employee.DesignationId);
            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.AllDesignations = await _context.Designations.OrderBy(d => d.Level).ThenBy(d => d.Name).ToListAsync();

            var selectedDesignation = await _context.Designations.FindAsync(employee.DesignationId);
            var selectedLevel = selectedDesignation?.Level ?? 999;
            var managers = await _context.Employees
                .Include(e => e.Designation)
                .Where(e => e.IsActive && e.Designation != null && e.Designation.Level < selectedLevel && e.Id != employee.Id)
                .ToListAsync();
            if (!managers.Any())
            {
                managers = await _context.Employees
                    .Include(e => e.Designation)
                    .Where(e => e.IsActive && e.Designation != null && e.Designation.Name == "Admin" && e.Id != employee.Id)
                    .ToListAsync();
            }
            ViewData["ManagerId"] = new SelectList(managers, "Id", "FullNameWithDesignation", employee.ManagerId);
            return View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> GetEligibleManagers(int designationId, int? currentEmployeeId = null)
        {
            var designation = await _context.Designations.FindAsync(designationId);
            if (designation == null) return BadRequest("Invalid designation.");

            // Find all active employees with a hierarchy level strictly higher (strictly smaller Level number)
            var managers = await _context.Employees
                .Include(e => e.Designation)
                .Where(e => e.IsActive && e.Designation != null && e.Designation.Level < designation.Level)
                .ToListAsync();

            if (currentEmployeeId.HasValue)
            {
                managers = managers.Where(e => e.Id != currentEmployeeId.Value).ToList();
            }

            if (!managers.Any())
            {
                // Default to Admin employee(s)
                managers = await _context.Employees
                    .Include(e => e.Designation)
                    .Where(e => e.IsActive && e.Designation != null && e.Designation.Name == "Admin" && (currentEmployeeId == null || e.Id != currentEmployeeId.Value))
                    .ToListAsync();
            }

            var result = managers.Select(m => new { id = m.Id, fullName = m.FullName, designationName = m.Designation?.Name });
            return Json(result);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv()
        {
            var items = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.Manager)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("EmployeeCode,FirstName,LastName,Email,Phone,DateOfBirth,FathersName,Gender,LocalAddress,PermanentAddress,DepartmentName,DesignationName,DateOfJoining,Salary,IsActive,ManagerCode");
            foreach (var item in items)
            {
                csv.AppendLine($"{Helpers.CsvHelper.Escape(item.EmployeeCode)}," +
                               $"{Helpers.CsvHelper.Escape(item.FirstName)}," +
                               $"{Helpers.CsvHelper.Escape(item.LastName)}," +
                               $"{Helpers.CsvHelper.Escape(item.Email)}," +
                               $"{Helpers.CsvHelper.Escape(item.Phone)}," +
                               $"{item.DateOfBirth?.ToString("yyyy-MM-dd")}," +
                               $"{Helpers.CsvHelper.Escape(item.FathersName)}," +
                               $"{item.Gender}," +
                               $"{Helpers.CsvHelper.Escape(item.LocalAddress)}," +
                               $"{Helpers.CsvHelper.Escape(item.PermanentAddress)}," +
                               $"{Helpers.CsvHelper.Escape(item.Department?.Name)}," +
                               $"{Helpers.CsvHelper.Escape(item.Designation?.Name)}," +
                               $"{item.DateOfJoining.ToString("yyyy-MM-dd")}," +
                               $"{item.Salary}," +
                               $"{item.IsActive}," +
                               $"{Helpers.CsvHelper.Escape(item.Manager?.EmployeeCode)}");
            }
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "employees.csv");
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
                var defaultDept = await _context.Departments.FirstOrDefaultAsync(d => d.IsActive);
                var defaultDesig = await _context.Designations.FirstOrDefaultAsync();

                using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
                {
                    var header = await reader.ReadLineAsync(); // skip header
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = Helpers.CsvHelper.ParseCsvLine(line);
                        if (parts.Count < 15) continue;

                        var code = parts[0].Trim();
                        var firstName = parts[1].Trim();
                        var lastName = parts[2].Trim();
                        var email = parts[3].Trim();
                        var phone = parts[4].Trim();
                        
                        DateTime? dob = null;
                        if (DateTime.TryParse(parts[5].Trim(), out var pDob)) dob = pDob;

                        var fathersName = parts[6].Trim();

                        Gender gender = Gender.Male;
                        Enum.TryParse<Gender>(parts[7].Trim(), true, out gender);

                        var localAddress = parts[8].Trim();
                        var permanentAddress = parts[9].Trim();
                        var deptName = parts[10].Trim();
                        var desigName = parts[11].Trim();

                        DateTime doj = DateTime.Today;
                        if (DateTime.TryParse(parts[12].Trim(), out var pDoj)) doj = pDoj;

                        decimal salary = 0;
                        decimal.TryParse(parts[13].Trim(), out salary);

                        bool isActive = true;
                        bool.TryParse(parts[14].Trim(), out isActive);

                        var managerCode = parts.Count >= 16 ? parts[15].Trim() : string.Empty;

                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(email)) continue;

                        // Resolve Dept
                        int deptId = defaultDept?.Id ?? 1;
                        if (!string.IsNullOrEmpty(deptName))
                        {
                            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == deptName.ToLower());
                            if (dept != null) deptId = dept.Id;
                        }

                        // Resolve Desig
                        int desigId = defaultDesig?.Id ?? 1;
                        if (!string.IsNullOrEmpty(desigName))
                        {
                            var desig = await _context.Designations.FirstOrDefaultAsync(d => d.Name.ToLower() == desigName.ToLower());
                            if (desig != null) desigId = desig.Id;
                        }

                        // Resolve Manager
                        int? managerId = null;
                        if (!string.IsNullOrEmpty(managerCode))
                        {
                            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == managerCode.ToLower());
                            managerId = manager?.Id;
                        }

                        var existing = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeCode.ToLower() == code.ToLower());
                        if (existing == null)
                        {
                            var emp = new Employee
                            {
                                EmployeeCode = code,
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email,
                                Phone = phone,
                                DateOfBirth = dob,
                                FathersName = fathersName,
                                Gender = gender,
                                LocalAddress = localAddress,
                                PermanentAddress = permanentAddress,
                                DepartmentId = deptId,
                                DesignationId = desigId,
                                DateOfJoining = doj,
                                Salary = salary,
                                IsActive = isActive,
                                ManagerId = managerId,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                            _context.Employees.Add(emp);
                        }
                        else
                        {
                            existing.FirstName = firstName;
                            existing.LastName = lastName;
                            existing.Phone = phone;
                            existing.DateOfBirth = dob;
                            existing.FathersName = fathersName;
                            existing.Gender = gender;
                            existing.LocalAddress = localAddress;
                            existing.PermanentAddress = permanentAddress;
                            existing.DepartmentId = deptId;
                            existing.DesignationId = desigId;
                            existing.DateOfJoining = doj;
                            existing.Salary = salary;
                            existing.IsActive = isActive;
                            existing.ManagerId = managerId;
                            existing.UpdatedAt = DateTime.Now;
                            _context.Employees.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Employees imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to import: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
