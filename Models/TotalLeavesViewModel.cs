using System;
using System.Collections.Generic;

namespace HRMSystem.Models
{
    public class TotalLeavesViewModel
    {
        public List<LeaveTypeColumn> LeaveTypes { get; set; } = new();
        public List<EmployeeLeaveRow> EmployeeRows { get; set; } = new();

        // Footer Totals across all filtered employees
        public Dictionary<int, LeaveCell> TypeTotals { get; set; } = new();
        public decimal GrandUsed { get; set; }
        public decimal GrandBalance { get; set; }
        public decimal GrandTotal { get; set; }

        // Filter selections
        public int? SelectedEmployeeId { get; set; }
        public int? SelectedMonth { get; set; }
        public int SelectedYear { get; set; } = DateTime.Now.Year;

        // Filter options for dropdowns
        public List<EmployeeOption> EmployeeOptions { get; set; } = new();
        public List<int> AvailableYears { get; set; } = new();
    }

    public class LeaveTypeColumn
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string BgColor { get; set; } = "#f8f9fa";
        public string BorderColor { get; set; } = "#dee2e6";
        public string TextColor { get; set; } = "#212529";
    }

    public class EmployeeLeaveRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;

        // Leaves mapped by LeaveTypeId
        public Dictionary<int, LeaveCell> LeavesByType { get; set; } = new();

        // "All together" totals
        public decimal TotalUsed { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalAllocated { get; set; }
    }

    public class LeaveCell
    {
        public decimal Used { get; set; }
        public decimal Balance { get; set; }
        public decimal Total { get; set; }
    }

    public class EmployeeOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
