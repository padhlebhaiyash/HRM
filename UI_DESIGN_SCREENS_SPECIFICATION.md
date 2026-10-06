# HRM System — Comprehensive UI/UX Design System & Screen Specifications
> **Target Tool**: Google Stitch / AI UI Design Generator / Project IDX  
> **Application**: Enterprise Human Resource Management (HRM) System  
> **Platform**: Responsive Web Application (Desktop-first, 1440px / Tablet 1024px / Mobile 390px)  
> **Version**: 2.0 (Production Architecture)

---

## 1. Global Design Tokens & Visual Language

### 1.1 Color Palette
- **Brand Primary**: `#0D6EFD` (Royal Blue) — Primary interactive buttons, key focus states, active badges.
- **Brand Accent / Corporate**: `#F15A24` (Energy Orange) — Header underline rule, highlights, high-priority notifications.
- **Brand Navy / Sidebar**: `#1E293B` (Slate Navy 900) & `#334155` (Slate Navy 700) — Admin sidebar & contrast headers.
- **ESS Slate Gray**: `#90A4AE` (Muted Steel Blue) — Employee portal top navbar active state and widget headers.
- **Neutral Background**: `#F8FAFC` (Cool Off-White) — Canvas background for dashboards.
- **Surface / Card Background**: `#FFFFFF` (Pure White) with `border: 1px solid #E2E8F0` and `border-radius: 12px`.
- **Typography Colors**:
  - Heading & Primary Text: `#0F172A` (Slate 900)
  - Secondary / Subtitle: `#64748B` (Slate 500)
  - Muted / Caption: `#94A3B8` (Slate 400)
- **Status & Feedback Accents**:
  - **Success / Present / Approved**: `#10B981` (Emerald Green) | Soft background: `#ECFDF5`
  - **Warning / Half-Day / Pending**: `#F59E0B` (Amber Orange) | Soft background: `#FFFBEB`
  - **Danger / Absent / Rejected**: `#EF4444` (Rose Red) | Soft background: `#FEF2F2`
  - **Info / Holiday / On Leave**: `#3B82F6` (Sky Blue) | Soft background: `#EFF6FF`

### 1.2 Typography & Elevation
- **Font Family**: Primary `'Open Sans', 'Inter', -apple-system, sans-serif`. Monospace for codes/amounts: `'JetBrains Mono', 'SFMono-Regular', monospace`.
- **Hierarchy**:
  - Page Titles: `28px`, Bold (700), Line-height: 1.25
  - Section Headers: `18px`, Semi-bold (600), Line-height: 1.3
  - Body Text: `14px`, Regular (400), Line-height: 1.5
  - Table / Meta / Badges: `12px`, Medium (500), Uppercase tracking `0.05em`
- **Shadows**:
  - Card Shadow: `0 1px 3px 0 rgba(0, 0, 0, 0.05), 0 1px 2px 0 rgba(0, 0, 0, 0.03)`
  - Elevated Dropdown / Modal: `0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.05)`

---

## 2. Layout Shells & Architectural Templates

### Layout Shell A: Modern Admin Portal (`_Layout`)
- **Left Sidebar Navigation** (Fixed, 260px width, Dark Slate `#1E293B` or Clean White `#FFFFFF` with border):
  - Brand Logo & Company Name with dynamic portal image.
  - Primary Sections: Dashboard, Employees Directory, Departments & Roles, Attendance Master, Leave Approvals, Leave Balances, Payroll Disbursement, Portal Settings.
  - Bottom pinned item: User mini-avatar, active role badge ("Administrator"), and Logout trigger.
- **Top Bar** (Sticky, height 64px, White `#FFFFFF`):
  - Quick Search input (`⌘ + K`), Branch / Location selector, Notifications bell with count badge (`3`), Profile Dropdown.
- **Main View Canvas**:
  - Max width `1400px`, padding `24px 32px`, breadcrumbs bar at top.

### Layout Shell B: Employee Self-Service (ESS) Portal (`_EmployeeDashboardLayout`)
- **Top Navigation Banner**:
  - Clean white header, distinct `2px solid #F15A24` bottom accent line.
  - Left: Logo & Company Name.
  - Right Tabs: `Dashboard`, `My Attendance`, `Apply Leave`, `My Leaves`, `My Payslips`, `Team Calendar`, `My Profile`.
- **Split-View Canvas**:
  - **Left Profile Widget** (300px width card): Headshot photo with rounded frame, Full Name, Employee Code badge (`EMP-042`), Designation, Department, Check-in status pill ("Punched In at 09:15 AM"), Quick links (Emergency Contacts, Reporting Manager info).
  - **Right Main Content Canvas** (Flexible flex-grow): Modular header `#90A4AE`, white body container with tabbed components.

### Layout Shell C: Authentication Centered (`_LoginLayout`)
- Full viewport `100vh`, split layout or centered 460px card on `#F1F5F9` background with subtle geometric background gradient.
- Centered company logo, clean security badge, and version footer.

---

## 3. Screen-by-Screen UI Specifications

### Module 1: Authentication & Access Control

#### Screen 01: Secure Login Portal
- **Route**: `/Auth/Login`
- **Purpose**: Unified multi-role authentication (Admin, Manager, Employee).
- **Layout**:
  - Centered elevated card (`440px` width) with `border-radius: 16px`, soft shadow.
  - Header: Corporate Logo image or stylized building icon, Title "Welcome Back", Subtitle "Sign in to access your HRM workspace".
- **Form Controls**:
  - **Username / Email Address**: Floating label input with prefix icon `bi-person-fill`.
  - **Password**: Floating input with eye toggle button `bi-eye-slash` to show/hide password text.
  - **Remember Me**: Custom styled checkbox.
  - **Forgot Password**: Right-aligned link in primary `#0D6EFD`.
  - **Sign In CTA**: Full-width high-contrast button with loading spinner state ("Signing in...").
- **Footer**: "Protected by Enterprise 2FA & SSL Encryption".

#### Screen 02: Forgot Password Request
- **Route**: `/Auth/ForgotPassword`
- **Purpose**: Self-service password recovery initiation.
- **Components**:
  - Illustration / Icon: Lock reset badge in soft blue circle.
  - Text: "Reset your password. Enter your registered company email address to receive a secure password recovery link."
  - Input: Email address field with instant email format regex validation.
  - Action Buttons: Primary "Send Reset Link", Secondary text link "← Back to Login".

#### Screen 03: Password Reset Link Dispatched
- **Route**: `/Auth/ForgotPasswordConfirmation`
- **Components**:
  - Success Icon: Green circular checkmark with pulse ring.
  - Message: "Check your inbox. If an account matches `user@company.com`, we've sent instructions to set up a new password."
  - Timer notice: "Link expires in 30 minutes for security."
  - Action Button: "Return to Sign In".

#### Screen 04: Set / Reset Password via Token
- **Route**: `/Auth/ResetPasswordViaToken?token={secureToken}`
- **Components**:
  - Input 1: "New Password" with interactive 4-segment strength meter (Length, Upper/Lower, Number, Symbol).
  - Input 2: "Confirm New Password" with green check indicator matching state.
  - CTA Button: "Save New Password & Continue".

#### Screen 05: Reset Link Expired Warning
- **Route**: `/Auth/ResetPasswordExpired`
- **Components**:
  - Warning Icon: Amber clock badge `bi-hourglass-split`.
  - Message: "This password reset token has expired or has already been used."
  - CTA: "Request a New Reset Link".

---

### Module 2: Executive & HR Administration

#### Screen 06: Admin Executive Dashboard
- **Route**: `/Admin/Dashboard`
- **Header**: Greeting "Welcome, System Admin" + Date indicator ("Tuesday, October 06, 2026") + "Export Summary" button.
- **Top Metric KPI Row** (4 Stat Cards):
  1. **Total Employees**: `148` (trend: `+4 this month`), icon: `bi-people-fill` in Blue.
  2. **Present Today**: `136 / 148` (`91.8%`), icon: `bi-check-circle-fill` in Green.
  3. **On Leave / Out Today**: `12`, icon: `bi-calendar-event` in Amber.
  4. **Pending Approvals**: `7` (Leaves: 5, Profile Edits: 2), icon: `bi-bell-fill` in Red.
- **Main Section Split Grid**:
  - **Left 65% Panel — Live Attendance Muster Today**:
    - Filter tabs: `All (148)`, `Present (136)`, `Late Punch (8)`, `Absent (4)`.
    - Compact live table: Employee Name, Code, Department, Punch In Time, Punch Location (Geo tag / Within Office), Status Pill.
  - **Right 35% Panel — Quick Action & Pending Queue**:
    - Leave Request approval cards with quick "Approve" / "Reject" inline actions.
    - Quick shortcut tiles: "Add New Employee", "Run Monthly Payroll", "Customize Portal".

#### Screen 07: Employee Directory & Roster
- **Route**: `/Employee/Index`
- **Toolbar**:
  - Search bar: "Search by name, employee code, email..."
  - Filter dropdowns: Department (`Engineering`, `HR`, `Finance`, `Operations`), Status (`Active`, `Probation`, `Terminated`).
  - Actions: "Export CSV" and "+ Add Employee" button in Brand Primary.
- **Data Table**:
  - Columns:
    - Employee (Avatar + Full Name + Code `EMP-019`)
    - Department & Designation (e.g. `Engineering` / `Senior Backend Developer`)
    - Work Email & Phone
    - Joining Date
    - Status Badge (`Active` [Green pill], `On Leave` [Blue pill])
    - Actions Dropdown (`View Dossier`, `Edit Profile`, `Reset Credentials`, `Deactivate`)
- **Footer**: Pagination (e.g. "Showing 1 to 20 of 148 entries") + page numbers.

#### Screen 08: Onboard New Employee Form
- **Route**: `/Employee/Create`
- **Layout**: Multi-card wizard format.
- **Card 1: Personal Information**:
  - Profile Photo Upload (Drag & drop zone with instant thumbnail preview).
  - First Name, Last Name, Date of Birth, Gender selector, Contact Number, Personal Email.
  - Current Address and Permanent Address with "Same as Current" checkbox.
- **Card 2: Job & Organizational Placement**:
  - Employee Code (Auto-generated or custom editable).
  - Department (Dropdown), Designation (Dropdown), Reporting Manager (Searchable select).
  - Employment Type (`Full-Time`, `Contractor`, `Intern`), Joining Date, Probation End Date.
- **Card 3: Compensation & Banking**:
  - Basic Monthly Salary, Bank Account Number, IFSC / Routing Code, PAN / National ID.
- **Card 4: Portal Credentials**:
  - System Username, Official Email (`user@company.com`), Initial Role (`Employee`, `Manager`, `Admin`), "Send Welcome Email with Setup Link" toggle.
- **Footer Bar**: Sticky bar with "Discard" and "Create Employee & Send Invitation" buttons.

#### Screen 09: Employee 360° Profile Dossier
- **Route**: `/Employee/Details/{id}`
- **Header Banner**: High-resolution profile avatar, Full Name, Designation, Department, Employee Code, and "Edit Profile" shortcut.
- **Tabbed Interface**:
  - **Tab 1: Overview**: Personal details, reporting hierarchy diagram, emergency contacts.
  - **Tab 2: Attendance Ledger**: Month-by-month calendar heat-map, total working hours, late arrivals count.
  - **Tab 3: Leave Quota & History**: Circular progress rings for Casual Leave, Sick Leave, Earned Leave with used vs remaining breakdown.
  - **Tab 4: Payroll & Salary**: Current CTC breakdown, downloadable monthly payslip links table.

#### Screen 10: Department Management
- **Route**: `/Department/Index`
- **Components**:
  - Grid of Department Cards or Data Table showing: Department Name, Department Head (Avatar + Name), Active Member Count, Actions (Edit, Delete).
  - "+ Create Department" modal trigger with inputs: Name, Code (`ENG`, `FIN`), Head of Department selector.

#### Screen 11: Designation & Role Hierarchy
- **Route**: `/Designation/Index`
- **Components**:
  - Organizational Tree / List view displaying: Role Title, Department tag, Hierarchy Level (`L1 Executive` to `L7 Director`), Number of employees currently holding the role.
  - Inline Edit / Add modal with validation.

#### Screen 12: Profile Change Request Approval Desk
- **Route**: `/Admin/ProfileRequests`
- **Purpose**: HR review for employee self-service contact or address updates.
- **UI Structure**:
  - Comparison Table with side-by-side diff:
    - Employee Name & Code
    - Field Modified (e.g., `Emergency Contact Phone`, `Residential Address`)
    - Current Registered Value (Red-tinted block)
    - Requested New Value (Green-tinted block)
    - Date of Request
    - Action buttons: "Approve Changes" (Applies to DB immediately) vs "Decline" with modal reason.

#### Screen 13: Annual Leave Ledger & YTD Breakdown
- **Route**: `/Admin/TotalLeaves`
- **Header**: Year Selector (`2026`), Department filter, "Export CSV" trigger.
- **Analytics Cards**: Total Organization Leaves Taken, Most Utilized Leave Category, Department with Highest Absenteeism.
- **Summary Matrix Table**:
  - Rows: Every employee.
  - Columns: Employee Code, Name, Department, Casual Leaves Taken, Sick Leaves Taken, Paid Leaves Taken, Unpaid / LOP Days, Total Leaves YTD, Remaining Available Balance.

#### Screen 14: White-Label Portal Customization
- **Route**: `/Admin/CustomizePortal`
- **Card 1: Company Identity**:
  - Company Legal Name input.
  - Company Logo upload with live preview on dark and light navigation bars.
  - "Clear Logo / Use Text Only" option.
- **Card 2: Application URL & System Domains**:
  - Application Base URL (e.g. `https://munrohr.inovexa.solutions`).
- **Card 3: Email Relay & Branding**:
  - Outgoing From Email (`techinovexasolutions@gmail.com`).
  - From Display Name (`HRM System`).
- **Save Actions**: Floating success toast "Settings updated successfully".

---

### Module 3: Location-Based Attendance with Live Photo Verification

#### Screen 15: Employee Web Check-In Console
- **Route**: `/Attendance/MyAttendance`
- **Layout**: Modern split widget dashboard.
- **Widget A: Live Clock & Geofencing Card**:
  - Large digital clock displaying live seconds (`09:41:25 AM`).
  - Current Date with calendar badge.
  - **Geofence Status Indicator**:
    - Radar / Map pin animation.
    - Green Badge: "Within Office Perimeter: Central Tech Park (Accuracy: 8m)" OR Amber Badge: "Outside Registered Office (Work From Home / Remote)".
- **Widget B: Live Photo Capture (Webcam Stream)**:
  - Video viewport (1:1 circular or rounded 4:3 box).
  - "Allow Camera Access" prompt if camera disabled.
  - Overlay target frame guide for facial alignment.
  - Capture button with camera shutter icon.
  - Review snapshot thumbnail before punching in.
- **Action Buttons**:
  - Large Dual Punch Buttons:
    - **"Check In"** (Vibrant Green `#10B981`) — enabled when not checked in.
    - **"Check Out"** (Vibrant Rose `#EF4444`) — enabled when checked in.
- **Widget C: Today's Work Summary**:
  - First In Time (`09:12 AM`), Last Out Time (`--:--`), Elapsed Active Hours (`4h 28m` live counter).
- **Lower Section**: Attendance History Table for the current month with Date, In, Out, Total Hours, Status Pill (`Present`, `Half Day`, `Late`), and thumbnail modal for captured punch photo.

#### Screen 16: Admin Organization Attendance Master
- **Route**: `/Attendance/AdminIndex`
- **Header**: Date Picker (Default: Today), Department Filter, Location Filter, Search.
- **Summary Metrics**: `Total Scheduled`, `Punched In`, `Late Arrivals`, `Missing Check-Outs`.
- **Master Attendance Table**:
  - Columns:
    - Employee Avatar, Name & Code
    - Date
    - Check-in Timestamp & Verified Location name
    - Check-in Photo (Clickable thumbnail triggering high-res proof modal)
    - Check-out Timestamp & Photo
    - Total Work Hours calculated
    - Status (`On Time`, `Late`, `Half Day`, `Absent`)
    - Manual HR Correction action trigger (pencil icon).

#### Screen 17: Geofence Office Premises Manager
- **Route**: `/Attendance/ManageLocations`
- **Purpose**: Configure physical offices and allowable GPS radii.
- **Components**:
  - Map View (interactive map with draggable pin and circle radius overlay).
  - List of Office Locations:
    - Location Name (e.g. `Headquarters - Building B`)
    - Coordinates (`Latitude: 18.5204`, `Longitude: 73.8567`)
    - Geofence Radius in Meters (e.g. `150m`)
    - Status Toggle (`Active` / `Inactive`)
    - Add New Location button with GPS coordinate auto-detect button ("Use Current GPS").

---

### Module 4: Leave Management & Quota System

#### Screen 18: Leave Application Modal / Form
- **Route**: `/Leave/Apply` or `/EmployeeSelf/ApplyLeave`
- **UI Elements**:
  - **Leave Type Selector**: Dropdown showing remaining quotas next to category names (e.g. `Casual Leave (Available: 6)`, `Sick Leave (Available: 4)`, `Earned Leave (Available: 12)`).
  - **Date Pickers**: Start Date and End Date with interactive date calendar.
  - **Day Option Toggle**: Radio group (`Full Day`, `First Half`, `Second Half`).
  - **Calculated Duration Pill**: Automatically computes and displays "Total Duration: 2.0 Working Days (Excluding Weekends)".
  - **Reason for Leave**: Textarea with character counter (Minimum 10 characters).
  - **Document Attachment**: Optional file upload for medical certificates on sick leave.
  - **Submission CTA**: "Submit Leave Application" with instant balance pre-check validation.

#### Screen 19: Employee Leave Ledger & History
- **Route**: `/EmployeeSelf/MyLeaves`
- **Top Balance Cards**: 4 Colored Pill Cards showing Allocated, Used, and Remaining for each leave category.
- **Filter Tabs**: `All Requests`, `Pending (2)`, `Approved (8)`, `Rejected (1)`.
- **History Table**:
  - Columns: Leave Type, Date Range, Total Days, Reason preview, Applied On, Status (`Pending` [Amber], `Approved` [Green], `Rejected` [Red]), Approver Name & Comments.
  - Actions: "Cancel Application" button available only for Pending requests.

#### Screen 20: Admin & Manager Leave Approval Desk
- **Route**: `/Leave/AdminIndex` and `/Manager/TeamLeaves`
- **Queue Table**:
  - Columns: Applicant Name & Code, Leave Category, Dates & Total Days, Reason, Remaining Balance info, Manager Recommendation, Action buttons.
  - **Approve Action**: One-click instant green button with success confirmation.
  - **Reject Action**: Red button opening `_RejectModal`:
    - Modal Header: "Reject Leave Request"
    - Form: Mandatory "Reason for Rejection" textarea (notifies employee via email with this explanation).
    - Modal CTA: "Confirm Rejection".

#### Screen 21: Organization Leave Balances Overview
- **Route**: `/LeaveBalance/Index`
- **Components**:
  - Table showing each employee's annual leave ledger with quick search.
  - "Adjust Balance" modal for HR admins to credit or debit annual leave days with audit note.

#### Screen 22: Leave Types & Policy Configuration
- **Route**: `/LeaveType/Index`
- **Components**:
  - Settings list for leave categories: Name, Annual Allowed Days, Paid / Unpaid toggle, Carry Forward to Next Year limit, Requires Document Proof flag.

---

### Module 5: Payroll & Compensation Management

#### Screen 23: Monthly Payroll Cycles Directory
- **Route**: `/Payroll/AdminIndex`
- **Header**: "+ Generate New Payroll" button.
- **Cycle Cards / Table**:
  - Columns: Month & Year (`October 2026`), Total Employees Processed (`148`), Total Gross (`$420,000`), Total Deductions (`$42,000`), Net Disbursement (`$378,000`), Status Badge (`Draft`, `Approved`, `Disbursed`), Action: "View Breakdown" / "Download Bank Disbursement File".

#### Screen 24: Process & Review Payroll Batch
- **Route**: `/Payroll/Generate`
- **Wizard Step 1**: Select Month and Year.
- **Wizard Step 2: Auto-Calculation Review Table**:
  - Auto-integrates with Attendance and Unpaid Leaves to compute Loss of Pay (LOP) days.
  - Columns: Employee Name, Base Salary, Attendance Days, LOP Deduction, Bonuses/Allowances, Tax Deduction, Net Payable.
  - Inline editable fields for one-time performance bonus or manual deduction.
- **CTA**: "Finalize & Generate Payslips".

#### Screen 25: Official Formal Pay Slip (PDF & Print View)
- **Route**: `/Payroll/PaySlip/{id}`
- **Layout**: Clean, print-ready document layout (`800px` printable frame, border, company header).
- **Header Section**: Company Name, Logo, Address, Tax ID, Month/Year label.
- **Employee Info Grid**: Employee Name, Code, Designation, Department, Bank Account, PAN/ID, Days in Month, Days Worked, LOP Days.
- **Earnings & Deductions Two-Column Ledger**:
  - **Left (Earnings)**: Basic Salary, House Rent Allowance (HRA), Special Allowance, Performance Bonus. Total Gross Earnings.
  - **Right (Deductions)**: Provident Fund (PF), Professional Tax, TDS / Income Tax, LOP Deduction. Total Deductions.
- **Net Salary Callout**: Large highlighted box: "NET PAYABLE: $4,850.00" + Amount in words.
- **Footer**: System verification hash, Computer-generated disclaimer, "Download PDF" and "Print Slip" floating action buttons.

#### Screen 26: Employee Self-Service My Compensation
- **Route**: `/EmployeeSelf/MyPayroll`
- **Components**:
  - Annual CTC summary card with tax breakdown chart.
  - List of past monthly payslips with Month, Net Pay, Disbursed Date, and "Download PDF" icon button.

---

### Module 6: Manager & Employee Self-Service (ESS)

#### Screen 27: Employee Self-Service Main Dashboard
- **Route**: `/EmployeeSelf/Dashboard`
- **Welcome Hero Card**: "Welcome, [Name] 👋" with quick status: "You are currently checked in since 09:15 AM".
- **Three-Column Grid**:
  - **Column 1 — Quick Attendance Widget**: Live clock, Check-In/Out buttons, week's attendance bar chart.
  - **Column 2 — Leave Balances Quick Cards**: Radial gauge cards for Casual, Sick, and Earned leaves with direct "Apply Leave" link.
  - **Column 3 — Upcoming Holidays & Team Out**: Card showing next 3 corporate public holidays and team members on leave this week.
- **Recent Payslips Row**: Quick card showing last month's payslip with download button.

#### Screen 28: Employee Profile & Change Request Console
- **Route**: `/EmployeeSelf/Profile`
- **Profile Card**: Avatar, Job Title, Department, Manager name, Work Email, Join Date.
- **Editable Information Section** with "Request Edit" buttons:
  - Phone Number, Residential Address, Emergency Contact Person & Phone.
  - Clicking "Request Edit" opens a modal where employee inputs updated data. Once submitted, it appears in HR Admin's Screen 12 queue.

#### Screen 29: Team Who's Out & Department Calendar
- **Route**: `/EmployeeSelf/TeamCalendar`
- **Components**:
  - FullCalendar view (Month / Week / Day view).
  - Color-coded badges for teammates:
    - 🟢 Present in Office
    - 🔵 On Approved Leave (with leave type label)
    - 🟡 Work From Home / Remote
    - 🟣 Official Public Holiday

#### Screen 30: Manager Team Pulse Dashboard
- **Route**: `/Manager/Dashboard`
- **Components**:
  - Team Size counter: `14 direct reports`.
  - Today's Team Status: `12 in office`, `1 on leave`, `1 WFH`.
  - Pending team leave applications awaiting manager approval.
  - Team monthly attendance punctuality score.

---

## 4. Reusable Component & Modal Catalog

1. **`_RejectModal`**:
   - Title: "Reject Request", prompt for rejection reason, red action confirmation.
2. **`_ValidationScriptsPartial`**:
   - Client-side live validation indicators (Green border for valid, Red text for error).
3. **Attendance Photo Viewer Modal**:
   - High-resolution modal popup showcasing punch photo, GPS coordinates, timestamp, and location match verification.
4. **Toast Notification System**:
   - Floating notifications at top-right for Success (Green), Warning (Yellow), Error (Red) with auto-dismiss after 4 seconds.
5. **Empty State Component**:
   - Clean illustration, Friendly title (e.g., "No Pending Leave Requests"), explanatory text, and optional action button.

---

## 5. Google Stitch AI Prompting Guide

To generate any screen from this specification in Google Stitch:
1. Copy the **Global Design Tokens (Section 1)** as the master style context.
2. Select the desired **Layout Shell (Section 2)**.
3. Paste the exact **Screen Specification (Section 3)**.
4. Add directive: *"Generate a high-fidelity, polished, modern enterprise web UI component matching these exact tokens, realistic mock data, and responsive layout."*
