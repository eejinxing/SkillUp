/* Skill UP - ONE-TIME fix: move old timestamps from UTC to the PC's local time.

   Why: before 2026-10-26 the backend saved times in UTC (8 hours behind a Malaysian PC).
   It now saves the PC's local time. Rows created earlier are still UTC; this script shifts them.

   Run it ONCE per database, BEFORE you use the updated site:
   1. Stop the site (Shift+F5 in Visual Studio).
   2. View > Server Explorer > right-click your SkillUp.mdf connection > New Query.
   3. Paste this whole file and press Ctrl+Shift+E.
   4. The Messages pane ends with "Done." and the Results pane lists the tables that were shifted.
   If it stops with an error, nothing is changed: fix the cause, then run the whole file again.

   Safe to re-run: a marker in the database makes a second run stop and change nothing.
   Not shifted: VOICE_CHANNEL.starts_at (typed by users as local time), plain date columns
   (course start date and deadline) and PASSWORD_RESET (reset links last only 15 minutes).
   Only values at or before the current UTC time are shifted, so rows written by the updated
   site (always later, because local time is ahead of UTC) are never shifted twice.
   Needs a PC whose time zone is ahead of UTC, such as Malaysia (UTC+8). */
-- A failed earlier run in the same query window leaves #shifted behind. Remove it in its own
-- batch, so the rest of this file is compiled without it.
IF OBJECT_ID(N'tempdb..#shifted') IS NOT NULL DROP TABLE #shifted;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'SkillUp.TimesShiftedToLocal')
BEGIN
    RAISERROR(N'This script was already run on this database. Nothing was changed.', 16, 1);
    RETURN;
END

DECLARE @offset INT = DATEDIFF(MINUTE, SYSUTCDATETIME(), SYSDATETIME());
IF @offset <= 0
BEGIN
    RAISERROR(N'This PC is not ahead of UTC, so this script does not apply. Nothing was changed.', 16, 1);
    RETURN;
END

DECLARE @cut DATETIME2 = SYSUTCDATETIME();
DECLARE @sql NVARCHAR(MAX) = N'';

-- One UPDATE per table that moves all its time columns together. Shifting them one at a time
-- would briefly break rules such as "submitted_at must not be earlier than started_at".
SELECT @sql += N'UPDATE ' + x.full_name + N' SET ' + x.set_list + N' WHERE ' + x.where_list + N';'
             + N' INSERT INTO #shifted VALUES (N''' + x.table_name + N''', @@ROWCOUNT);' + NCHAR(10)
FROM (
    SELECT QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) AS full_name,
           t.name AS table_name,
           STRING_AGG(CAST(QUOTENAME(c.name) + N' = CASE WHEN ' + QUOTENAME(c.name) + N' <= @cut THEN DATEADD(MINUTE, @m, ' + QUOTENAME(c.name) + N') ELSE ' + QUOTENAME(c.name) + N' END' AS NVARCHAR(MAX)), N', ') AS set_list,
           STRING_AGG(CAST(QUOTENAME(c.name) + N' <= @cut' AS NVARCHAR(MAX)), N' OR ') AS where_list
    FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE ty.name = N'datetime2'
      AND t.name <> N'PASSWORD_RESET'
      AND NOT (t.name = N'VOICE_CHANNEL' AND c.name = N'starts_at')
    GROUP BY s.name, t.name
) x;

CREATE TABLE #shifted (table_name NVARCHAR(200), rows_shifted INT);

BEGIN TRANSACTION;
EXEC sp_executesql @sql, N'@m INT, @cut DATETIME2', @m = @offset, @cut = @cut;

DECLARE @note NVARCHAR(200) = N'Shifted +' + CAST(@offset AS NVARCHAR(10)) + N' minutes on ' + CONVERT(NVARCHAR(30), SYSDATETIME(), 120);
EXEC sp_addextendedproperty @name = N'SkillUp.TimesShiftedToLocal', @value = @note;
COMMIT TRANSACTION;

SELECT table_name, rows_shifted FROM #shifted WHERE rows_shifted > 0 ORDER BY table_name;
DROP TABLE #shifted;
PRINT N'Done. Times were moved ' + CAST(@offset / 60.0 AS NVARCHAR(10)) + N' hours forward.';
