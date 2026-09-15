# Context: UI Redesign, Presentation Elevation & Universal Date Standardization

## Requirements & Scope
- User Request:
  - Modify the design, improve presentation, make interface more attractive and user-friendly.
  - Fix catastrophic date recorded issues across backend: what gets saved differs from what is displayed.
  - Standardize date handling across the board, uniform format and 100% accurate, correct data.
- Files to Touch:
  - `Social.Infrastructure/Data/ApplicationDbContext.cs`
  - `Social/Serialization/UtcDateTimeJsonConverter.cs` [NEW]
  - `Social/Program.cs`
  - `Social/Controllers/Admin/AdminDashboardController.cs`
  - `Social.Admin.Web/wwwroot/css/admin-dashboard.css`
  - `Social.Admin.Web/wwwroot/js/admin-dashboard.js`
  - `Social.Tests/Unit/Serialization/DateTimeStandardizationTests.cs` [NEW]
- Dependencies Added: None.
