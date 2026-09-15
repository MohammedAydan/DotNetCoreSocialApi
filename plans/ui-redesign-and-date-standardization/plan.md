# Feature Plan: UI Redesign, Presentation Elevation & Universal Date Standardization

## Goal
Completely overhaul and elevate the Admin Dashboard interface into a visually stunning, highly intuitive, and modern experience that eliminates irregular card heights and poor media presentation, while universally standardizing date and time handling across the entire backend (EF Core, MySQL, JSON serialization) and frontend to guarantee 100% accurate, consistent, and uniform date records.

## Acceptance Criteria

### 1. Universal Backend & API Date Standardization
- All EF Core entity properties of type `DateTime` and `DateTime?` are mapped with a universal UTC `ValueConverter`, ensuring every value stored to MySQL is UTC and every value read from MySQL has `DateTimeKind.Utc`.
- JSON serialization in ASP.NET Core (`Program.cs`) is configured with dedicated `UtcDateTimeJsonConverter` and `NullableUtcDateTimeJsonConverter` producing strictly compliant ISO 8601 strings with the `Z` UTC indicator (`yyyy-MM-ddTHH:mm:ss.fffZ`).
- Date parsing across all endpoints automatically normalizes incoming ISO dates to UTC.

### 2. Frontend Date Presentation & Format Uniformity
- All timestamps across the Admin Dashboard (Moderation, Users, Audit Logs, Overview) use a uniform, unambiguous format:
  - Concise display: `MMM DD, YYYY · HH:mm UTC` (e.g. `Sep 15, 2026 · 04:50 UTC`) alongside smart relative timestamps (`"Just now"`, `"5m ago"`, `"2h ago"`, `"Yesterday"`).
  - Hover tooltips reveal both exact UTC and local client time: `title="UTC: 2026-09-15 04:50:36 | Local: 2026-09-15 07:50:36"`.
  - Elimination of ambiguous American-style numeric dates (like `2/6/2026`).

### 3. Moderation Interface & Feed Layout Redesign
- **Rhythm & Height Regularization**:
  - Clamped content preview (max height ~180px–200px) with elegant gradient fadeout and inline "Read more / Show less" toggle.
  - No 3000px runaway cards that break columns and create visual chaos.
- **View Modes**:
  - **Feed Stream View (Default)**: Modern social feed layout (max-width 720px, centered) allowing natural, comfortable reading of posts, code blocks, and media.
  - **Uniform Grid View**: Multi-column cards with strictly enforced uniform card heights and clamped previews.
  - **Compact Table View**: High-density scannable list for rapid batch auditing.
- **Media Presentation**:
  - 1 Image: Full-width / 16:9 banner with rounded-xl corners, smooth hover zoom, and lightbox preview.
  - 2 Images: Side-by-side balanced split grid.
  - 3+ Images: Modern mosaic grid with `+N more` overlay badge on the final item.
  - Video: Sleek video wrapper with native controls and smooth aspect ratio preservation.
- **Typography & Internationalization (i18n)**:
  - Code blocks wrapped in a dark syntax container with language badge.
  - Arabic and multilingual content properly formatted with `dir="auto"` and appropriate typographic line-height.

### 4. Quality & Architecture Integrity
- Clean Architecture strictly maintained across Domain, Application, Infrastructure, API, and Web UI.
- All 149 existing tests continue to pass with 0 regressions.
- New unit tests added to verify UTC serialization and converter accuracy.
- Clean build in Release mode (0 compiler errors).

## Scope
- **IN SCOPE:**
  - `ApplicationDbContext.cs`: Universal UTC `ValueConverter` for all `DateTime` and `DateTime?` properties.
  - `Social/Program.cs` & `Social.API`: Custom `UtcDateTimeJsonConverter` and `NullableUtcDateTimeJsonConverter`.
  - `Social/Controllers/Admin/AdminDashboardController.cs`: Complete UI overhaul of the Moderation interface, date formatting helper, view mode toggles, clamped card containers, media mosaic layout, and code block formatting.
  - `Social.Admin.Web/wwwroot/css/admin-dashboard.css` and `admin-dashboard.js`: Updated styling, layout classes, and interaction scripts.
  - `Social.Tests`: Unit tests for date serialization and EF Core UTC conversion.
- **OUT OF SCOPE:**
  - Database schema alterations (all columns remain standard MySQL datetime/timestamp).

## Estimated Complexity
Medium (M)
