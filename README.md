# 🏢 Modern HRM System (Human Resource Management)

A comprehensive, enterprise-ready **Human Resource Management System** built with **ASP.NET Core 9 MVC**, **Entity Framework Core**, **Bootstrap 5**, and **HTML5 Geolocation API**.

![ASP.NET Core 9](https://img.shields.io/badge/ASP.NET%20Core-9.0-blue)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-9.0-green)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-purple)
![License](https://img.shields.io/badge/License-MIT-orange)

---

## 🌟 Key Features

### 📍 Location-Based Clock In & Clock Out (Geofencing)
- **GPS Perimeter Verification**: Employees can only Clock In and Clock Out when physically located within the designated office perimeter.
- **Haversine Distance Algorithm**: Calculates exact real-time distance in meters between user GPS coordinates and target office coordinates.
- **Live Location Status Banner**: Real-time feedback on employee dashboard indicating whether the user is inside or outside the permitted office zone.
- **Admin Geofence Location Manager**: Configure multiple office locations with custom latitude, longitude, address, allowed radius (in meters), and strict enforcement toggles.
- **Attendance Logs**: Detailed logs tracking clock-in time, clock-out time, total work duration, coordinates, and GPS verification badges.

### 👥 Employee & Department Management
- Manage full employee lifecycle: Employee Code, Name, Email, Phone, Date of Birth, Joining Date, Base Salary, and Profile Photo uploads.
- Reporting Manager hierarchy & organizational tree structure.
- Department & Designation management with level hierarchies.

### 🌴 Leave & Attendance System
- Multiple leave types: **Casual Leave (CL)**, **Sick Leave (SL)**, **Earned Leave (EL)**, **Unpaid Leave**, and **Work From Home (WFH)**.
- Automatic leave balance tracking per employee.
- Smart validation preventing past-dated applications, overlapping date requests, or public holiday conflicts.
- Interactive **Team Calendar** powered by FullCalendar.

### 💰 Payroll Management
- Monthly payroll processing with gross salary, earnings, tax/provident deductions, and net pay computation.
- Printable employee payslips.

### 🔒 Role-Based Access Control (RBAC)
- Multi-tier portal access for **Admin**, **Manager**, **HR**, and **Employee** roles.
- Dedicated Employee Self-Service portal (`EmployeeSelfController`) and Admin Operations portal (`AdminController`).

### 🎨 Portal Customization & Notice Board
- Company logo & title customization.
- Public holiday management with custom calendar colors.
- Interactive Notice Board with CSV Export/Import capabilities.

---

## 🛠️ Technology Stack

* **Backend**: C# ASP.NET Core 9.0 MVC
* **Database**: Entity Framework Core 9 (Code-First with Migrations), SQL Server / LocalDB
* **Frontend**: Razor Views, HTML5, CSS3, JavaScript (ES6+), jQuery
* **UI Framework**: Bootstrap 5, Bootstrap Icons
* **Geofencing**: HTML5 Geolocation API & C# Haversine Algorithm Implementation

---

## 🚀 Getting Started

### Prerequisites
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* SQL Server (LocalDB or Express / Full Edition)
* Visual Studio 2022 or VS Code

### Installation & Setup

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/padhlebhaiyash/HRM.git
   cd HRM/HRMSystem
   ```

2. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

3. **Apply Database Migrations**:
   ```bash
   dotnet ef database update
   ```

4. **Run the Project**:
   ```bash
   dotnet run
   ```

5. Open your web browser and navigate to:
   ```
   http://localhost:5035
   ```

---

## 📁 Project Structure

```
HRMSystem/
├── Controllers/
│   ├── AdminController.cs          # Admin Portal & Portal Customizations
│   ├── AttendanceController.cs     # Location-based Clock In/Out & Geofencing
│   ├── AuthController.cs           # User Login, Registration, Password Reset
│   ├── DepartmentController.cs     # Department Management
│   ├── DesignationController.cs    # Designation Hierarchy
│   ├── EmployeeController.cs       # Employee Operations
│   ├── EmployeeSelfController.cs   # Employee Self-Service & Dashboard
│   ├── LeaveController.cs          # Leave Application & Approvals
│   ├── LeaveTypeController.cs      # Leave Type Configurations
│   └── PayrollController.cs        # Payroll Processing & Payslips
├── Data/
│   ├── ApplicationDbContext.cs     # EF Core DbContext & Seed Data
│   └── DbSeeder.cs                 # Initial Admin & Role Seeding
├── Helpers/
│   ├── LocationHelper.cs           # Haversine GPS Distance Calculator
│   └── CsvHelper.cs                # CSV Exporter/Importer
├── Models/
│   ├── Attendance.cs               # Attendance Log Entity
│   ├── OfficeLocation.cs           # Office Geofence Location Entity
│   ├── Employee.cs                 # Employee Master Model
│   ├── LeaveRequest.cs             # Leave Request Model
│   └── PayrollRecord.cs            # Payroll Record Model
└── Views/                          # Razor View Templates
```

---

## 📄 License

This project is licensed under the MIT License.
