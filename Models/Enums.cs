namespace HRMSystem.Models
{
    public enum Gender
    {
        Male,
        Female,
        Other
    }

    public enum UserRole
    {
        // Removed: Using DB Table RBAC
    }


    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum PayrollStatus
    {
        Draft,
        Processed,
        Paid
    }

    public enum HalfDayPeriod
    {
        None,
        FirstHalf,
        SecondHalf
    }
}
