/* Skill UP - clear all data and start again from the demo data.

   Deletes every row of every table except ROLE (accounts, courses, enquiries, messages, ... including anything typed
   while testing) and restarts the ID counters. On its next start the web app finds no accounts and loads the demo data
   again from Database/demo-seed.json, so the site is back to a fresh install: the demo accounts, programs and courses
   with the same IDs as before (Course?id=7 is the same course again).

   1. Stop the site (Shift+F5 in Visual Studio).
   2. Copy App_Data\SkillUp.mdf and SkillUp_log.ldf somewhere safe if you may want the current data back.
   3. View > Server Explorer > right-click your SkillUp.mdf connection > New Query.
   4. Paste this whole file and press Ctrl+Shift+E. The Results pane lists how many rows each table had.
   5. Right-click the connection > Close Connection, then start the site (F5). It loads the demo data on the first page.
   If it stops with an error, nothing is changed. */
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'ROLE') IS NULL
BEGIN
    RAISERROR(N'This database has no Skill UP tables. Nothing was changed.', 16, 1);
    RETURN;
END

DECLARE @counts TABLE (table_name SYSNAME, rows_deleted INT);
DECLARE @sql NVARCHAR(MAX) = N'';

-- Count first, so the result shows what was there.
SELECT @sql += N'INSERT INTO @counts SELECT N''' + t.name + N''', COUNT(*) FROM ' + QUOTENAME(t.name) + N';' + NCHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> N'ROLE';
DECLARE @countSql NVARCHAR(MAX) = N'DECLARE @counts TABLE (table_name SYSNAME, rows_deleted INT);' + NCHAR(10) + @sql + N'SELECT * FROM @counts;';
INSERT INTO @counts EXEC (@countSql);

BEGIN TRANSACTION;

-- Foreign keys are switched off while the tables are emptied, so the order does not matter, then switched back on.
SET @sql = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + NCHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0;
EXEC (@sql);

SET @sql = N'';
SELECT @sql += N'DELETE FROM ' + QUOTENAME(t.name) + N';' + NCHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> N'ROLE';
EXEC (@sql);

SET @sql = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + NCHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0;
EXEC (@sql);

-- New rows get IDs from 1 again (tables that never had a row already start at 1).
SET @sql = N'';
SELECT @sql += N'DBCC CHECKIDENT (N''' + QUOTENAME(t.name) + N''', RESEED, 0) WITH NO_INFOMSGS;' + NCHAR(10)
FROM sys.tables t JOIN sys.identity_columns ic ON ic.object_id = t.object_id
WHERE t.is_ms_shipped = 0 AND t.name <> N'ROLE' AND ic.last_value IS NOT NULL;
EXEC (@sql);

COMMIT TRANSACTION;

SELECT table_name, rows_deleted FROM @counts WHERE rows_deleted > 0 ORDER BY table_name;
PRINT N'Done. Start the site: it loads the demo data again.';
