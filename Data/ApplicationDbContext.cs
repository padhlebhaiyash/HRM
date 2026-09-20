using HRMSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<BankDetail> BankDetails { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<PayrollRecord> PayrollRecords { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRoleMapping> UserRoles { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<CompanySetting> CompanySettings { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<OfficeLocation> OfficeLocations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default Office Location
            modelBuilder.Entity<OfficeLocation>().HasData(
                new OfficeLocation
                {
                    Id = 1,
                    Name = "Main Headquarters Office",
                    Address = "Corporate Hub, Main Office Site",
                    Latitude = 23.0225,
                    Longitude = 72.5714,
                    AllowedRadiusMeters = 300.0,
                    IsActive = true,
                    IsEnforced = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                }
            );

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite key for many-to-many UserRole mappings
            modelBuilder.Entity<UserRoleMapping>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Hierarchy tracking - prevent cascade deletes that would wipe out entire departments
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dynamically seed roles upon EF creation inherently avoiding missing dependencies
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "High Level Administrator" },
                new Role { Id = 2, Name = "Manager", Description = "Direct Reporting Manager" },
                new Role { Id = 3, Name = "HR", Description = "Human Resource Operations" },
                new Role { Id = 4, Name = "Employee", Description = "Standard Base Employee" }
            );

            // Dynamically seed leave types upon EF creation
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { Id = 1, Name = "Casual Leave", Code = "Casual", IsActive = true },
                new LeaveType { Id = 2, Name = "Sick Leave", Code = "Sick", IsActive = true },
                new LeaveType { Id = 3, Name = "Earned Leave", Code = "Earned", IsActive = true },
                new LeaveType { Id = 4, Name = "Unpaid Leave", Code = "Unpaid", IsActive = true },
                new LeaveType { Id = 5, Name = "Work From Home", Code = "WFH", IsActive = true }
            );

            modelBuilder.Entity<Designation>()
                .HasOne(d => d.ParentDesignation)
                .WithMany()
                .HasForeignKey(d => d.ParentDesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Designation>().HasData(
                new Designation { Id = 1, Name = "Admin", Level = 1, ParentDesignationId = null },
                new Designation { Id = 2, Name = "Manager", Level = 2, ParentDesignationId = 1 },
                new Designation { Id = 3, Name = "Team Lead", Level = 3, ParentDesignationId = 2 },
                new Designation { Id = 4, Name = "Executive", Level = 4, ParentDesignationId = 3 }
            );

            // Configure Delete Behavior
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Designation)
                .WithMany()
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.LeaveType)
                .WithMany()
                .HasForeignKey(l => l.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollRecord>()
                .HasOne(p => p.Employee)
                .WithMany(e => e.PayrollRecords)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique index constraints
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.BankDetail)
                .WithOne(b => b.Employee)
                .HasForeignKey<BankDetail>(b => b.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveBalances)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(l => l.LeaveType)
                .WithMany()
                .HasForeignKey(l => l.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
