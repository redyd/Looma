# Update

## Statistics

- Redesigned the **Statistics** page with two tabs: **Yarn** and **Overview**.
- Yarn tab: key figures (yarn used with a trend vs the previous period, yarn added, current stock, projects supplied), a chart of yarn used and added over time, breakdowns by material and yarn weight, current stock by yarn weight, and the most used yarns and most yarn-hungry projects.
- Overview tab: projects finished (with trend), started, average duration and total, a chart of projects started and finished over time, breakdowns by status and technique, the longest projects, and a summary of your patterns, yarns and documents.
- Charts now adapt to the selected period (per day, week, month or year) and show values per period instead of running totals.
- Deleting a yarn or a project no longer erases its consumption history: past stock movements keep the yarn and project details they were recorded with.

## Backup & Restore

- Added a **Data & backups** section in the settings.
- Export all your data (yarns, patterns, projects, documents, images, themes and preferences) into a single `.looma` file.
- Import a backup to restore everything. The file is fully checked before anything is changed, a safety backup of your current data is created first, and Looma restarts to apply it.
- Looma now backs up your data automatically before each database update (the 5 most recent are kept).
- Added a **Reset the application** button in the settings, protected by a double confirmation. Your data is backed up automatically before being erased.

## Data Safety

- Looma no longer crashes when its database is damaged: a recovery screen lets you restore a backup, import one, or start over while keeping the damaged file aside.
- A damaged preferences file is now reset automatically instead of blocking the app (a copy is kept).
- Stock changes, project completion and multi-document imports are now saved all at once or not at all, so an error can no longer leave half-applied changes.
- Deleting a pattern, project or document no longer risks losing its files if the deletion fails.
- Invalid numbers (empty or impossible quantities) are now rejected before being saved.
- Theme files are checked before being imported.

## Data Check

- Added a **Check my data** action: it detects documents whose file is missing, unused files, and invalid themes.
- Documents with a missing file now show a "File missing" badge and can no longer be opened by mistake.
- Unused files can be moved to a quarantine folder and documents without a file can be removed in one click.

## Other

- Updated dependencies (Avalonia 12.1, Entity Framework Core 10.0.12, Velopack 1.2).
