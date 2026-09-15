# Tasks: UI Redesign, Presentation Elevation & Universal Date Standardization

- [x] Task 1: Backend Date Handling & Universal UTC Standardization
  - [x] Implement universal UTC `ValueConverter` in `Social.Infrastructure/Data/ApplicationDbContext.cs` for all `DateTime` and `DateTime?` entity properties
  - [x] Implement `UtcDateTimeJsonConverter` and `NullableUtcDateTimeJsonConverter` in `Social.API` and register them in `Program.cs` `AddJsonOptions`
  - [x] Add unit tests verifying UTC serialization, ISO 8601 formatting with `Z`, and date normalization
- [x] Task 2: Moderation Dashboard UI & Layout Redesign
  - [x] Redesign post card container in `AdminDashboardController.cs` with clamped height (gradient fadeout and "Read more / Show less" toggle)
  - [x] Implement View Switcher: Feed Stream View (clean single column social stream), Uniform Grid View, and Compact Table View
  - [x] Overhaul media rendering: 1-image banner, 2-image split, 3+ image mosaic with `+N` badge, and sleek HTML5 video container
  - [x] Implement code block syntax styling and multilingual bidirectional text support (`dir="auto"`) for Arabic content
- [x] Task 3: Frontend Date Standardization & User Experience Elevation
  - [x] Implement universal JavaScript date formatting function `formatStandardDate(isoString)`: formatted as `MMM DD, YYYY · HH:mm UTC` + smart relative time (`"2m ago"`, `"1h ago"`, `"Yesterday"`) with hover tooltips showing both UTC and Local timestamps
  - [x] Update all date displays across Moderation cards, User tables, and Audit Log tables to use the standard formatter
  - [x] Refine header stats pills and filter toolbar styling
- [x] Task 4: Testing, Verification & Closure
  - [x] Run full test suite (`dotnet test Social.sln -c Release` -> 160/160 passing)
  - [x] Verify 0 compiler errors (`dotnet build Social.sln -c Release`)
  - [x] Update `plans/context.md`, `plans/SESSION_LOG.md`, and write `review.md`
