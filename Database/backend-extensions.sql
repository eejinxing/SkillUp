/* Skill UP - backend extensions on top of create-tables.sql.

   The ERD tables hold the core records. The web UI also needs a few fields and history
   tables the ERD does not have (course schedule and colour, lesson type, enquiry access
   codes and which browser may open them, course request history, course files,
   announcement comments, review replies).
   This script adds them without changing or removing anything the ERD defines.

   You do not need to run it by hand: the web app applies it automatically on its first
   API request (Backend/DatabaseSetup.cs), and Database/setup-db.ps1 runs it too.
   It is safe to run again; every step checks first. Batches are separated by GO. */

IF OBJECT_ID(N'ROLE') IS NULL
    RAISERROR(N'Run Database/create-tables.sql first.', 16, 1);
GO

/* USER: profile fields used by the profile page. profile_picture holds an uploaded image as a data URL, so it needs MAX. */
IF COL_LENGTH(N'[USER]', N'location') IS NULL ALTER TABLE [USER] ADD location NVARCHAR(100) NULL;
IF COL_LENGTH(N'[USER]', N'theme') IS NULL ALTER TABLE [USER] ADD theme NVARCHAR(10) NOT NULL CONSTRAINT DF_USER_theme DEFAULT N'dark';
IF COL_LENGTH(N'[USER]', N'preferences') IS NULL ALTER TABLE [USER] ADD preferences NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'[USER]', N'profile_picture') <> -1 ALTER TABLE [USER] ALTER COLUMN profile_picture NVARCHAR(MAX) NULL;
GO

/* COURSE: schedule and card styling. */
IF COL_LENGTH(N'COURSE', N'start_date') IS NULL ALTER TABLE COURSE ADD start_date DATE NULL;
IF COL_LENGTH(N'COURSE', N'deadline') IS NULL ALTER TABLE COURSE ADD deadline DATE NULL;
IF COL_LENGTH(N'COURSE', N'duration_weeks') IS NULL ALTER TABLE COURSE ADD duration_weeks INT NOT NULL CONSTRAINT DF_COURSE_duration_weeks DEFAULT 1;
IF COL_LENGTH(N'COURSE', N'color') IS NULL ALTER TABLE COURSE ADD color NVARCHAR(20) NULL;
IF COL_LENGTH(N'COURSE', N'icon') IS NULL ALTER TABLE COURSE ADD icon NVARCHAR(10) NULL;
GO

/* LAB rows are the course's ordered learning activities: an article, a lab task or the course quiz. */
IF COL_LENGTH(N'LAB', N'lesson_type') IS NULL
    ALTER TABLE LAB ADD lesson_type NVARCHAR(10) NOT NULL CONSTRAINT DF_LAB_lesson_type DEFAULT N'article'
        CONSTRAINT CK_LAB_lesson_type CHECK (lesson_type IN (N'article', N'lab', N'quiz'));
IF COL_LENGTH(N'LAB', N'minutes') IS NULL ALTER TABLE LAB ADD minutes INT NOT NULL CONSTRAINT DF_LAB_minutes DEFAULT 15;
GO

IF COL_LENGTH(N'QUIZ_QUESTION', N'topic') IS NULL ALTER TABLE QUIZ_QUESTION ADD topic NVARCHAR(100) NULL;
IF COL_LENGTH(N'ENROLLMENT', N'last_active') IS NULL ALTER TABLE ENROLLMENT ADD last_active DATETIME2 NULL;
IF COL_LENGTH(N'ACTIVITY_LOG', N'detail') IS NULL ALTER TABLE ACTIVITY_LOG ADD detail NVARCHAR(500) NULL;
IF COL_LENGTH(N'VOICE_CHANNEL', N'starts_at') IS NULL ALTER TABLE VOICE_CHANNEL ADD starts_at DATETIME2 NULL;
/* Only a SHA-256 hash of the enquiry access code is stored. */
IF COL_LENGTH(N'ENQUIRY', N'access_token_hash') IS NULL ALTER TABLE ENQUIRY ADD access_token_hash NVARCHAR(64) NULL;
GO

/* "My enquiries": which browser may open which enquiry without retyping its access code.
   The browser keeps only a random key in an HttpOnly cookie; this table stores a SHA-256 hash of it. */
IF OBJECT_ID(N'ENQUIRY_ACCESS') IS NULL
CREATE TABLE ENQUIRY_ACCESS (
    access_id         INT IDENTITY(1,1) NOT NULL,
    visitor_key_hash  NVARCHAR(64)      NOT NULL,
    enquiry_id        INT               NOT NULL,
    granted_at        DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_ENQUIRY_ACCESS PRIMARY KEY (access_id),
    CONSTRAINT FK_ENQUIRY_ACCESS_enquiry_id FOREIGN KEY (enquiry_id) REFERENCES ENQUIRY (enquiry_id) ON DELETE CASCADE,
    CONSTRAINT UQ_ENQUIRY_ACCESS_visitor_enquiry UNIQUE (visitor_key_hash, enquiry_id)
);
GO

/* Moderators can also flag a post (proposal: "Removes or flags inappropriate posts"). */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DISCUSSION_POST_status' AND definition NOT LIKE N'%Flagged%')
BEGIN
    ALTER TABLE DISCUSSION_POST DROP CONSTRAINT CK_DISCUSSION_POST_status;
    ALTER TABLE DISCUSSION_POST ADD CONSTRAINT CK_DISCUSSION_POST_status CHECK (status IN (N'Visible', N'Hidden', N'Removed', N'Flagged'));
END
GO

/* Course files attached by staff. The demo stores small files (up to 2 MB) as data URLs. */
IF OBJECT_ID(N'COURSE_RESOURCE') IS NULL
CREATE TABLE COURSE_RESOURCE (
    resource_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    uploaded_by INT NOT NULL,
    file_name NVARCHAR(255) NOT NULL,
    size_label NVARCHAR(50) NULL,
    data_url NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_COURSE_RESOURCE PRIMARY KEY (resource_id),
    CONSTRAINT FK_COURSE_RESOURCE_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id) ON DELETE CASCADE,
    CONSTRAINT FK_COURSE_RESOURCE_uploaded_by FOREIGN KEY (uploaded_by) REFERENCES [USER] (user_id)
);
GO

/* Create / Delete requests that instructors send to the Program Leader, with their timeline.
   course_id, program_id and user IDs have no foreign key on purpose: the history must survive
   after an approved deletion removes the course, or after an account is deleted. */
IF OBJECT_ID(N'COURSE_REQUEST') IS NULL
CREATE TABLE COURSE_REQUEST (
    request_id INT IDENTITY(1,1) NOT NULL,
    kind NVARCHAR(10) NOT NULL,
    course_id INT NOT NULL,
    course_title NVARCHAR(200) NOT NULL,
    program_id INT NULL,
    requester_id INT NULL,
    status NVARCHAR(30) NOT NULL DEFAULT N'Pending review',
    reason NVARCHAR(2000) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_COURSE_REQUEST PRIMARY KEY (request_id),
    CONSTRAINT CK_COURSE_REQUEST_kind CHECK (kind IN (N'Create', N'Delete')),
    CONSTRAINT CK_COURSE_REQUEST_status CHECK (status IN (N'Pending review', N'Approved', N'Changes requested', N'Rejected', N'Withdrawn'))
);
GO

IF OBJECT_ID(N'COURSE_REQUEST_EVENT') IS NULL
CREATE TABLE COURSE_REQUEST_EVENT (
    event_id INT IDENTITY(1,1) NOT NULL,
    request_id INT NOT NULL,
    actor_id INT NULL,
    action NVARCHAR(30) NOT NULL,
    comment NVARCHAR(2000) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_COURSE_REQUEST_EVENT PRIMARY KEY (event_id),
    CONSTRAINT FK_COURSE_REQUEST_EVENT_request_id FOREIGN KEY (request_id) REFERENCES COURSE_REQUEST (request_id) ON DELETE CASCADE,
    CONSTRAINT CK_COURSE_REQUEST_EVENT_action CHECK (action IN (N'Submitted', N'Review started', N'Approved', N'Changes requested', N'Rejected', N'Withdrawn'))
);
GO

IF OBJECT_ID(N'ANNOUNCEMENT_COMMENT') IS NULL
CREATE TABLE ANNOUNCEMENT_COMMENT (
    comment_id INT IDENTITY(1,1) NOT NULL,
    announcement_id INT NOT NULL,
    user_id INT NOT NULL,
    content NVARCHAR(1000) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_ANNOUNCEMENT_COMMENT PRIMARY KEY (comment_id),
    CONSTRAINT FK_ANNOUNCEMENT_COMMENT_announcement_id FOREIGN KEY (announcement_id) REFERENCES ANNOUNCEMENT (announcement_id) ON DELETE CASCADE,
    CONSTRAINT FK_ANNOUNCEMENT_COMMENT_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id)
);
GO

/* Replies to a course review (course reviews are WEBSITE_FEEDBACK rows with a course_id). */
IF OBJECT_ID(N'FEEDBACK_REPLY') IS NULL
CREATE TABLE FEEDBACK_REPLY (
    reply_id INT IDENTITY(1,1) NOT NULL,
    feedback_id INT NOT NULL,
    user_id INT NOT NULL,
    content NVARCHAR(2000) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_FEEDBACK_REPLY PRIMARY KEY (reply_id),
    CONSTRAINT FK_FEEDBACK_REPLY_feedback_id FOREIGN KEY (feedback_id) REFERENCES WEBSITE_FEEDBACK (feedback_id) ON DELETE CASCADE,
    CONSTRAINT FK_FEEDBACK_REPLY_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id)
);
GO

PRINT N'Done: backend extensions applied.';
