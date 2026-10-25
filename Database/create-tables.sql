/* Skill UP - create all 37 tables from the ERD (docs/proposal/WAPP.drawio).

   How to set up your database in Visual Studio:
   1. View > Server Explorer. Right-click Data Connections > Add Connection...
   2. Data source: Microsoft SQL Server Database File. Database file name: browse to this
      project's App_Data folder and type SkillUp.mdf. Click OK, then Yes to create it.
   3. Right-click the new SkillUp.mdf connection > New Query.
   4. Paste this whole file and press Ctrl+Shift+E. It ends with "Done: 37 tables created."
   5. Right-click Tables > Refresh.

   Run it once. Running it again stops with a message and changes nothing.
   Note: USER is a reserved word, so write it as [USER] in queries. */
SET XACT_ABORT ON;
IF OBJECT_ID(N'ROLE') IS NOT NULL
BEGIN
    RAISERROR(N'The tables already exist in this database. Nothing was changed.', 16, 1);
    RETURN;
END
BEGIN TRANSACTION;

CREATE TABLE ROLE (
    role_id INT IDENTITY(1,1) NOT NULL,
    role_name NVARCHAR(50) NOT NULL,
    description NVARCHAR(255) NULL,
    CONSTRAINT PK_ROLE PRIMARY KEY (role_id),
    CONSTRAINT UQ_ROLE_role_name UNIQUE (role_name)
);

CREATE TABLE [USER] (
    user_id INT IDENTITY(1,1) NOT NULL,
    role_id INT NOT NULL,
    first_name NVARCHAR(50) NOT NULL,
    last_name NVARCHAR(50) NOT NULL,
    username NVARCHAR(50) NOT NULL,
    password NVARCHAR(255) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    profile_picture NVARCHAR(500) NULL,
    bio NVARCHAR(1000) NULL,
    preferred_language NVARCHAR(10) NOT NULL DEFAULT N'en',
    account_status NVARCHAR(20) NOT NULL DEFAULT N'Active',
    total_points INT NOT NULL DEFAULT 0,
    current_streak INT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    last_login DATETIME2 NULL,
    CONSTRAINT PK_USER PRIMARY KEY (user_id),
    CONSTRAINT FK_USER_role_id FOREIGN KEY (role_id) REFERENCES ROLE (role_id),
    CONSTRAINT UQ_USER_username UNIQUE (username),
    CONSTRAINT UQ_USER_email UNIQUE (email),
    CONSTRAINT CK_USER_account_status CHECK (account_status IN (N'Active', N'Pending', N'Inactive')),
    CONSTRAINT CK_USER_total_points CHECK (total_points >= 0),
    CONSTRAINT CK_USER_current_streak CHECK (current_streak >= 0)
);

CREATE TABLE INSTRUCTOR_PROFILE (
    instructor_profile_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    expertise NVARCHAR(200) NULL,
    qualification NVARCHAR(200) NULL,
    teaching_experience NVARCHAR(500) NULL,
    certification NVARCHAR(500) NULL,
    verification_status NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    verified_by INT NULL,
    verified_at DATETIME2 NULL,
    CONSTRAINT PK_INSTRUCTOR_PROFILE PRIMARY KEY (instructor_profile_id),
    CONSTRAINT FK_INSTRUCTOR_PROFILE_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_INSTRUCTOR_PROFILE_verified_by FOREIGN KEY (verified_by) REFERENCES [USER] (user_id),
    CONSTRAINT UQ_INSTRUCTOR_PROFILE_user_id UNIQUE (user_id),
    CONSTRAINT CK_INSTRUCTOR_PROFILE_verification_status CHECK (verification_status IN (N'Pending', N'Approved', N'Rejected'))
);

CREATE TABLE PASSWORD_RESET (
    reset_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    token_hash NVARCHAR(255) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    expires_at DATETIME2 NOT NULL,
    is_used BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_PASSWORD_RESET PRIMARY KEY (reset_id),
    CONSTRAINT FK_PASSWORD_RESET_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id) ON DELETE CASCADE,
    CONSTRAINT CK_PASSWORD_RESET_expires_at CHECK (expires_at > created_at)
);

CREATE TABLE PROGRAM (
    program_id INT IDENTITY(1,1) NOT NULL,
    program_leader_id INT NULL,
    program_name NVARCHAR(150) NOT NULL,
    description NVARCHAR(1000) NULL,
    status NVARCHAR(20) NOT NULL DEFAULT N'Draft',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_PROGRAM PRIMARY KEY (program_id),
    CONSTRAINT FK_PROGRAM_program_leader_id FOREIGN KEY (program_leader_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_PROGRAM_status CHECK (status IN (N'Draft', N'Published'))
);

CREATE TABLE COURSE (
    course_id INT IDENTITY(1,1) NOT NULL,
    program_id INT NOT NULL,
    created_by INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    description NVARCHAR(2000) NULL,
    difficulty_level NVARCHAR(20) NOT NULL DEFAULT N'Beginner',
    skill_gained NVARCHAR(1000) NULL,
    thumbnail NVARCHAR(500) NULL,
    visibility_status NVARCHAR(20) NOT NULL DEFAULT N'Visible',
    approval_status NVARCHAR(30) NOT NULL DEFAULT N'Draft',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2 NULL,
    CONSTRAINT PK_COURSE PRIMARY KEY (course_id),
    CONSTRAINT FK_COURSE_program_id FOREIGN KEY (program_id) REFERENCES PROGRAM (program_id),
    CONSTRAINT FK_COURSE_created_by FOREIGN KEY (created_by) REFERENCES [USER] (user_id),
    CONSTRAINT CK_COURSE_difficulty_level CHECK (difficulty_level IN (N'Beginner', N'Intermediate', N'Advanced')),
    CONSTRAINT CK_COURSE_visibility_status CHECK (visibility_status IN (N'Visible', N'Hidden')),
    CONSTRAINT CK_COURSE_approval_status CHECK (approval_status IN (N'Draft', N'Pending review', N'Changes requested', N'Rejected', N'Published'))
);

CREATE TABLE COURSE_INSTRUCTOR (
    course_instructor_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    instructor_id INT NOT NULL,
    assigned_by INT NULL,
    assigned_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_COURSE_INSTRUCTOR PRIMARY KEY (course_instructor_id),
    CONSTRAINT FK_COURSE_INSTRUCTOR_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id) ON DELETE CASCADE,
    CONSTRAINT FK_COURSE_INSTRUCTOR_instructor_id FOREIGN KEY (instructor_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_COURSE_INSTRUCTOR_assigned_by FOREIGN KEY (assigned_by) REFERENCES [USER] (user_id),
    CONSTRAINT UQ_COURSE_INSTRUCTOR_course_instructor UNIQUE (course_id, instructor_id)
);

CREATE TABLE CONTENT_REVIEW (
    review_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    reviewer_id INT NOT NULL,
    decision NVARCHAR(30) NOT NULL,
    comments NVARCHAR(2000) NULL,
    reviewed_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_CONTENT_REVIEW PRIMARY KEY (review_id),
    CONSTRAINT FK_CONTENT_REVIEW_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id) ON DELETE CASCADE,
    CONSTRAINT FK_CONTENT_REVIEW_reviewer_id FOREIGN KEY (reviewer_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_CONTENT_REVIEW_decision CHECK (decision IN (N'Approved', N'Rejected', N'Changes requested'))
);

CREATE TABLE LEARNING_PATH (
    path_id INT IDENTITY(1,1) NOT NULL,
    program_id INT NOT NULL,
    created_by INT NOT NULL,
    path_name NVARCHAR(150) NOT NULL,
    description NVARCHAR(1000) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_LEARNING_PATH PRIMARY KEY (path_id),
    CONSTRAINT FK_LEARNING_PATH_program_id FOREIGN KEY (program_id) REFERENCES PROGRAM (program_id),
    CONSTRAINT FK_LEARNING_PATH_created_by FOREIGN KEY (created_by) REFERENCES [USER] (user_id)
);

CREATE TABLE LEARNING_PATH_COURSE (
    path_course_id INT IDENTITY(1,1) NOT NULL,
    path_id INT NOT NULL,
    course_id INT NOT NULL,
    sequence_no INT NOT NULL,
    is_starting_course BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_LEARNING_PATH_COURSE PRIMARY KEY (path_course_id),
    CONSTRAINT FK_LEARNING_PATH_COURSE_path_id FOREIGN KEY (path_id) REFERENCES LEARNING_PATH (path_id) ON DELETE CASCADE,
    CONSTRAINT FK_LEARNING_PATH_COURSE_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT UQ_LEARNING_PATH_COURSE_path_course UNIQUE (path_id, course_id),
    CONSTRAINT UQ_LEARNING_PATH_COURSE_path_sequence UNIQUE (path_id, sequence_no),
    CONSTRAINT CK_LEARNING_PATH_COURSE_sequence_no CHECK (sequence_no > 0)
);

CREATE TABLE ENROLLMENT (
    enrollment_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    course_id INT NOT NULL,
    enrolled_by INT NULL,
    enroll_date DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    status NVARCHAR(20) NOT NULL DEFAULT N'In progress',
    progress_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    completed_at DATETIME2 NULL,
    CONSTRAINT PK_ENROLLMENT PRIMARY KEY (enrollment_id),
    CONSTRAINT FK_ENROLLMENT_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_ENROLLMENT_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT FK_ENROLLMENT_enrolled_by FOREIGN KEY (enrolled_by) REFERENCES [USER] (user_id),
    CONSTRAINT UQ_ENROLLMENT_user_course UNIQUE (user_id, course_id),
    CONSTRAINT CK_ENROLLMENT_status CHECK (status IN (N'In progress', N'Completed', N'Dropped')),
    CONSTRAINT CK_ENROLLMENT_progress_percent CHECK (progress_percent BETWEEN 0 AND 100)
);

CREATE TABLE LAB (
    lab_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    article_content NVARCHAR(MAX) NULL,
    sequence_no INT NOT NULL,
    is_preview BIT NOT NULL DEFAULT 0,
    status NVARCHAR(20) NOT NULL DEFAULT N'Draft',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_LAB PRIMARY KEY (lab_id),
    CONSTRAINT FK_LAB_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT CK_LAB_status CHECK (status IN (N'Draft', N'Published')),
    CONSTRAINT CK_LAB_sequence_no CHECK (sequence_no > 0)
);

CREATE TABLE LAB_TASK (
    task_id INT IDENTITY(1,1) NOT NULL,
    lab_id INT NOT NULL,
    question_text NVARCHAR(2000) NOT NULL,
    task_type NVARCHAR(20) NOT NULL DEFAULT N'Text',
    correct_answer NVARCHAR(1000) NULL,
    starter_code NVARCHAR(MAX) NULL,
    expected_output NVARCHAR(MAX) NULL,
    points INT NOT NULL DEFAULT 1,
    sequence_no INT NOT NULL,
    CONSTRAINT PK_LAB_TASK PRIMARY KEY (task_id),
    CONSTRAINT FK_LAB_TASK_lab_id FOREIGN KEY (lab_id) REFERENCES LAB (lab_id) ON DELETE CASCADE,
    CONSTRAINT CK_LAB_TASK_task_type CHECK (task_type IN (N'Text', N'Code')),
    CONSTRAINT CK_LAB_TASK_points CHECK (points >= 0)
);

CREATE TABLE LAB_SUBMISSION (
    submission_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    lab_id INT NOT NULL,
    score DECIMAL(5,2) NULL,
    result_status NVARCHAR(20) NOT NULL DEFAULT N'Submitted',
    submitted_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_LAB_SUBMISSION PRIMARY KEY (submission_id),
    CONSTRAINT FK_LAB_SUBMISSION_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_LAB_SUBMISSION_lab_id FOREIGN KEY (lab_id) REFERENCES LAB (lab_id),
    CONSTRAINT CK_LAB_SUBMISSION_result_status CHECK (result_status IN (N'Submitted', N'Passed', N'Failed'))
);

CREATE TABLE TASK_ANSWER (
    task_answer_id INT IDENTITY(1,1) NOT NULL,
    submission_id INT NOT NULL,
    task_id INT NOT NULL,
    user_answer NVARCHAR(MAX) NULL,
    code_output NVARCHAR(MAX) NULL,
    is_correct BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_TASK_ANSWER PRIMARY KEY (task_answer_id),
    CONSTRAINT FK_TASK_ANSWER_submission_id FOREIGN KEY (submission_id) REFERENCES LAB_SUBMISSION (submission_id) ON DELETE CASCADE,
    CONSTRAINT FK_TASK_ANSWER_task_id FOREIGN KEY (task_id) REFERENCES LAB_TASK (task_id),
    CONSTRAINT UQ_TASK_ANSWER_submission_task UNIQUE (submission_id, task_id)
);

CREATE TABLE QUIZ (
    quiz_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    created_by INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    time_limit_minutes INT NULL,
    is_ai_generated BIT NOT NULL DEFAULT 0,
    status NVARCHAR(20) NOT NULL DEFAULT N'Draft',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_QUIZ PRIMARY KEY (quiz_id),
    CONSTRAINT FK_QUIZ_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT FK_QUIZ_created_by FOREIGN KEY (created_by) REFERENCES [USER] (user_id),
    CONSTRAINT CK_QUIZ_status CHECK (status IN (N'Draft', N'Published')),
    CONSTRAINT CK_QUIZ_time_limit_minutes CHECK (time_limit_minutes IS NULL OR time_limit_minutes > 0)
);

CREATE TABLE QUIZ_QUESTION (
    question_id INT IDENTITY(1,1) NOT NULL,
    quiz_id INT NOT NULL,
    question_text NVARCHAR(1000) NOT NULL,
    question_type NVARCHAR(30) NOT NULL DEFAULT N'Multiple choice',
    points INT NOT NULL DEFAULT 1,
    sequence_no INT NOT NULL,
    CONSTRAINT PK_QUIZ_QUESTION PRIMARY KEY (question_id),
    CONSTRAINT FK_QUIZ_QUESTION_quiz_id FOREIGN KEY (quiz_id) REFERENCES QUIZ (quiz_id) ON DELETE CASCADE,
    CONSTRAINT CK_QUIZ_QUESTION_question_type CHECK (question_type IN (N'Multiple choice', N'True/False')),
    CONSTRAINT CK_QUIZ_QUESTION_points CHECK (points >= 0)
);

CREATE TABLE QUESTION_OPTION (
    option_id INT IDENTITY(1,1) NOT NULL,
    question_id INT NOT NULL,
    option_text NVARCHAR(500) NOT NULL,
    is_correct BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_QUESTION_OPTION PRIMARY KEY (option_id),
    CONSTRAINT FK_QUESTION_OPTION_question_id FOREIGN KEY (question_id) REFERENCES QUIZ_QUESTION (question_id) ON DELETE CASCADE
);

CREATE TABLE QUIZ_ATTEMPT (
    attempt_id INT IDENTITY(1,1) NOT NULL,
    quiz_id INT NOT NULL,
    user_id INT NOT NULL,
    score DECIMAL(5,2) NULL,
    started_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    submitted_at DATETIME2 NULL,
    CONSTRAINT PK_QUIZ_ATTEMPT PRIMARY KEY (attempt_id),
    CONSTRAINT FK_QUIZ_ATTEMPT_quiz_id FOREIGN KEY (quiz_id) REFERENCES QUIZ (quiz_id),
    CONSTRAINT FK_QUIZ_ATTEMPT_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_QUIZ_ATTEMPT_score CHECK (score IS NULL OR score BETWEEN 0 AND 100),
    CONSTRAINT CK_QUIZ_ATTEMPT_submitted_at CHECK (submitted_at IS NULL OR submitted_at >= started_at)
);

CREATE TABLE ATTEMPT_ANSWER (
    attempt_answer_id INT IDENTITY(1,1) NOT NULL,
    attempt_id INT NOT NULL,
    question_id INT NOT NULL,
    selected_option_id INT NULL,
    is_correct BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_ATTEMPT_ANSWER PRIMARY KEY (attempt_answer_id),
    CONSTRAINT FK_ATTEMPT_ANSWER_attempt_id FOREIGN KEY (attempt_id) REFERENCES QUIZ_ATTEMPT (attempt_id) ON DELETE CASCADE,
    CONSTRAINT FK_ATTEMPT_ANSWER_question_id FOREIGN KEY (question_id) REFERENCES QUIZ_QUESTION (question_id),
    CONSTRAINT FK_ATTEMPT_ANSWER_selected_option_id FOREIGN KEY (selected_option_id) REFERENCES QUESTION_OPTION (option_id),
    CONSTRAINT UQ_ATTEMPT_ANSWER_attempt_question UNIQUE (attempt_id, question_id)
);

CREATE TABLE BADGE (
    badge_id INT IDENTITY(1,1) NOT NULL,
    badge_name NVARCHAR(100) NOT NULL,
    description NVARCHAR(500) NULL,
    icon NVARCHAR(500) NULL,
    criteria NVARCHAR(500) NULL,
    CONSTRAINT PK_BADGE PRIMARY KEY (badge_id),
    CONSTRAINT UQ_BADGE_badge_name UNIQUE (badge_name)
);

CREATE TABLE USER_BADGE (
    user_badge_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    badge_id INT NOT NULL,
    earned_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_USER_BADGE PRIMARY KEY (user_badge_id),
    CONSTRAINT FK_USER_BADGE_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id) ON DELETE CASCADE,
    CONSTRAINT FK_USER_BADGE_badge_id FOREIGN KEY (badge_id) REFERENCES BADGE (badge_id) ON DELETE CASCADE,
    CONSTRAINT UQ_USER_BADGE_user_badge UNIQUE (user_id, badge_id)
);

CREATE TABLE ANNOUNCEMENT (
    announcement_id INT IDENTITY(1,1) NOT NULL,
    posted_by INT NOT NULL,
    course_id INT NULL,
    program_id INT NULL,
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2 NULL,
    CONSTRAINT PK_ANNOUNCEMENT PRIMARY KEY (announcement_id),
    CONSTRAINT FK_ANNOUNCEMENT_posted_by FOREIGN KEY (posted_by) REFERENCES [USER] (user_id),
    CONSTRAINT FK_ANNOUNCEMENT_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT FK_ANNOUNCEMENT_program_id FOREIGN KEY (program_id) REFERENCES PROGRAM (program_id)
);

CREATE TABLE DISCUSSION_POST (
    post_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    course_id INT NULL,
    lab_id INT NULL,
    parent_post_id INT NULL,
    content NVARCHAR(MAX) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT N'Visible',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_DISCUSSION_POST PRIMARY KEY (post_id),
    CONSTRAINT FK_DISCUSSION_POST_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_DISCUSSION_POST_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT FK_DISCUSSION_POST_lab_id FOREIGN KEY (lab_id) REFERENCES LAB (lab_id),
    CONSTRAINT FK_DISCUSSION_POST_parent_post_id FOREIGN KEY (parent_post_id) REFERENCES DISCUSSION_POST (post_id),
    CONSTRAINT CK_DISCUSSION_POST_status CHECK (status IN (N'Visible', N'Hidden', N'Removed'))
);

CREATE TABLE TAG (
    tag_id INT IDENTITY(1,1) NOT NULL,
    tag_name NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_TAG PRIMARY KEY (tag_id),
    CONSTRAINT UQ_TAG_tag_name UNIQUE (tag_name)
);

CREATE TABLE POST_TAG (
    post_tag_id INT IDENTITY(1,1) NOT NULL,
    post_id INT NOT NULL,
    tag_id INT NOT NULL,
    CONSTRAINT PK_POST_TAG PRIMARY KEY (post_tag_id),
    CONSTRAINT FK_POST_TAG_post_id FOREIGN KEY (post_id) REFERENCES DISCUSSION_POST (post_id) ON DELETE CASCADE,
    CONSTRAINT FK_POST_TAG_tag_id FOREIGN KEY (tag_id) REFERENCES TAG (tag_id) ON DELETE CASCADE,
    CONSTRAINT UQ_POST_TAG_post_tag UNIQUE (post_id, tag_id)
);

CREATE TABLE MODERATION_LOG (
    moderation_id INT IDENTITY(1,1) NOT NULL,
    post_id INT NOT NULL,
    moderator_id INT NOT NULL,
    action NVARCHAR(30) NOT NULL,
    reason NVARCHAR(500) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_MODERATION_LOG PRIMARY KEY (moderation_id),
    CONSTRAINT FK_MODERATION_LOG_post_id FOREIGN KEY (post_id) REFERENCES DISCUSSION_POST (post_id),
    CONSTRAINT FK_MODERATION_LOG_moderator_id FOREIGN KEY (moderator_id) REFERENCES [USER] (user_id)
);

CREATE TABLE CHAT_MESSAGE (
    message_id INT IDENTITY(1,1) NOT NULL,
    sender_id INT NOT NULL,
    receiver_id INT NOT NULL,
    content NVARCHAR(4000) NOT NULL,
    is_read BIT NOT NULL DEFAULT 0,
    sent_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_CHAT_MESSAGE PRIMARY KEY (message_id),
    CONSTRAINT FK_CHAT_MESSAGE_sender_id FOREIGN KEY (sender_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_CHAT_MESSAGE_receiver_id FOREIGN KEY (receiver_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_CHAT_MESSAGE_sender_receiver CHECK (sender_id <> receiver_id)
);

CREATE TABLE AI_CHAT_SESSION (
    session_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    started_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_AI_CHAT_SESSION PRIMARY KEY (session_id),
    CONSTRAINT FK_AI_CHAT_SESSION_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id) ON DELETE CASCADE
);

CREATE TABLE AI_CHAT_MESSAGE (
    ai_message_id INT IDENTITY(1,1) NOT NULL,
    session_id INT NOT NULL,
    sender_type NVARCHAR(10) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_AI_CHAT_MESSAGE PRIMARY KEY (ai_message_id),
    CONSTRAINT FK_AI_CHAT_MESSAGE_session_id FOREIGN KEY (session_id) REFERENCES AI_CHAT_SESSION (session_id) ON DELETE CASCADE,
    CONSTRAINT CK_AI_CHAT_MESSAGE_sender_type CHECK (sender_type IN (N'User', N'AI'))
);

CREATE TABLE VOICE_CHANNEL (
    channel_id INT IDENTITY(1,1) NOT NULL,
    course_id INT NOT NULL,
    created_by INT NOT NULL,
    channel_name NVARCHAR(100) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT N'Open',
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_VOICE_CHANNEL PRIMARY KEY (channel_id),
    CONSTRAINT FK_VOICE_CHANNEL_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT FK_VOICE_CHANNEL_created_by FOREIGN KEY (created_by) REFERENCES [USER] (user_id),
    CONSTRAINT CK_VOICE_CHANNEL_status CHECK (status IN (N'Open', N'Closed'))
);

CREATE TABLE CHANNEL_PARTICIPANT (
    participant_id INT IDENTITY(1,1) NOT NULL,
    channel_id INT NOT NULL,
    user_id INT NOT NULL,
    camera_enabled BIT NOT NULL DEFAULT 0,
    joined_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    left_at DATETIME2 NULL,
    CONSTRAINT PK_CHANNEL_PARTICIPANT PRIMARY KEY (participant_id),
    CONSTRAINT FK_CHANNEL_PARTICIPANT_channel_id FOREIGN KEY (channel_id) REFERENCES VOICE_CHANNEL (channel_id) ON DELETE CASCADE,
    CONSTRAINT FK_CHANNEL_PARTICIPANT_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_CHANNEL_PARTICIPANT_left_at CHECK (left_at IS NULL OR left_at >= joined_at)
);

CREATE TABLE NOTIFICATION (
    notification_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    type NVARCHAR(50) NOT NULL,
    message NVARCHAR(1000) NOT NULL,
    link NVARCHAR(500) NULL,
    is_read BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_NOTIFICATION PRIMARY KEY (notification_id),
    CONSTRAINT FK_NOTIFICATION_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id) ON DELETE CASCADE
);

CREATE TABLE WEBSITE_FEEDBACK (
    feedback_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    course_id INT NULL,
    rating TINYINT NOT NULL,
    comment NVARCHAR(2000) NULL,
    is_public BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_WEBSITE_FEEDBACK PRIMARY KEY (feedback_id),
    CONSTRAINT FK_WEBSITE_FEEDBACK_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT FK_WEBSITE_FEEDBACK_course_id FOREIGN KEY (course_id) REFERENCES COURSE (course_id),
    CONSTRAINT CK_WEBSITE_FEEDBACK_rating CHECK (rating BETWEEN 1 AND 5)
);

CREATE TABLE ENQUIRY (
    enquiry_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NULL,
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    subject NVARCHAR(200) NOT NULL,
    message NVARCHAR(4000) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT N'Open',
    submitted_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2 NULL,
    CONSTRAINT PK_ENQUIRY PRIMARY KEY (enquiry_id),
    CONSTRAINT FK_ENQUIRY_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id),
    CONSTRAINT CK_ENQUIRY_status CHECK (status IN (N'Open', N'In progress', N'Resolved', N'Closed'))
);

CREATE TABLE FAQ (
    faq_id INT IDENTITY(1,1) NOT NULL,
    question NVARCHAR(500) NOT NULL,
    answer NVARCHAR(2000) NOT NULL,
    category NVARCHAR(50) NULL,
    CONSTRAINT PK_FAQ PRIMARY KEY (faq_id)
);

CREATE TABLE ACTIVITY_LOG (
    log_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NULL,
    action NVARCHAR(100) NOT NULL,
    target_type NVARCHAR(50) NULL,
    target_id INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_ACTIVITY_LOG PRIMARY KEY (log_id),
    CONSTRAINT FK_ACTIVITY_LOG_user_id FOREIGN KEY (user_id) REFERENCES [USER] (user_id)
);

INSERT INTO ROLE (role_name, description) VALUES
    (N'Learner', N'Enrols in courses, completes activities and quizzes.'),
    (N'Instructor', N'Creates and teaches courses, tracks learner performance.'),
    (N'Program Leader', N'Manages programs and approves course content.'),
    (N'Admin', N'Manages user accounts and the platform.');

COMMIT TRANSACTION;
PRINT N'Done: 37 tables created.';
