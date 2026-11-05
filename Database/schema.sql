-- Skill UP: PostgreSQL schema (40 tables). Run once on an empty database; the app does it for you on first start.
--
-- Conventions
--   * Table and column names are lower-case snake_case. The old [USER] table is app_user, because USER is reserved.
--   * Primary keys are identity columns; times are timestamptz (stored in UTC, shown in the server's local time).
--   * Free text is TEXT; a few short fields keep a VARCHAR limit that matches the validation in Services/Commands*.cs.

-- ---- Accounts --------------------------------------------------------------------------------------------------------

CREATE TABLE role (
    role_id     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    role_name   VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

CREATE TABLE app_user (
    user_id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    role_id            INT NOT NULL REFERENCES role (role_id),
    first_name         VARCHAR(50)  NOT NULL,
    last_name          VARCHAR(50)  NOT NULL,
    username           VARCHAR(50)  NOT NULL UNIQUE,
    password           VARCHAR(255) NOT NULL,           -- PBKDF2$iterations$salt$hash
    email              VARCHAR(255) NOT NULL UNIQUE,
    profile_picture    TEXT,                            -- an uploaded image is kept as a data URL
    bio                VARCHAR(1000),
    preferred_language VARCHAR(10)  NOT NULL DEFAULT 'en',
    account_status     VARCHAR(20)  NOT NULL DEFAULT 'Active' CHECK (account_status IN ('Active', 'Pending', 'Inactive')),
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT now(),
    last_login         TIMESTAMPTZ,
    location           VARCHAR(100),
    theme              VARCHAR(10)  NOT NULL DEFAULT 'dark',
    preferences        TEXT                             -- JSON
);

CREATE TABLE instructor_profile (
    instructor_profile_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id             INT NOT NULL UNIQUE REFERENCES app_user (user_id),
    verification_status VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (verification_status IN ('Pending', 'Approved', 'Rejected')),
    verified_by         INT REFERENCES app_user (user_id),
    verified_at         TIMESTAMPTZ
);

CREATE TABLE password_reset (
    reset_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id    INT NOT NULL REFERENCES app_user (user_id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at TIMESTAMPTZ NOT NULL,
    is_used    BOOLEAN NOT NULL DEFAULT false,
    CHECK (expires_at > created_at)
);

-- ---- Programs and courses --------------------------------------------------------------------------------------------

CREATE TABLE program (
    program_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    program_leader_id INT REFERENCES app_user (user_id),
    program_name      VARCHAR(150) NOT NULL,
    description       VARCHAR(1000),
    status            VARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Published')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE course (
    course_id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    program_id        INT NOT NULL REFERENCES program (program_id),
    created_by        INT NOT NULL REFERENCES app_user (user_id),
    title             VARCHAR(200) NOT NULL,
    description       VARCHAR(2000),
    difficulty_level  VARCHAR(20) NOT NULL DEFAULT 'Beginner' CHECK (difficulty_level IN ('Beginner', 'Intermediate', 'Advanced')),
    skill_gained      VARCHAR(1000),
    thumbnail         VARCHAR(500),
    visibility_status VARCHAR(20) NOT NULL DEFAULT 'Visible' CHECK (visibility_status IN ('Visible', 'Hidden')),
    approval_status   VARCHAR(30) NOT NULL DEFAULT 'Draft'
                      CHECK (approval_status IN ('Draft', 'Pending review', 'Changes requested', 'Rejected', 'Published')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ,
    start_date        DATE,
    deadline          DATE,
    duration_weeks    INT NOT NULL DEFAULT 1,
    color             VARCHAR(20),
    icon              VARCHAR(10)
);

CREATE TABLE course_instructor (
    course_instructor_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id     INT NOT NULL REFERENCES course (course_id) ON DELETE CASCADE,
    instructor_id INT NOT NULL REFERENCES app_user (user_id),
    assigned_by   INT REFERENCES app_user (user_id),
    assigned_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (course_id, instructor_id)
);

CREATE TABLE content_review (
    review_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id   INT NOT NULL REFERENCES course (course_id) ON DELETE CASCADE,
    reviewer_id INT NOT NULL REFERENCES app_user (user_id),
    decision    VARCHAR(30) NOT NULL CHECK (decision IN ('Approved', 'Rejected', 'Changes requested')),
    comments    VARCHAR(2000),
    reviewed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE learning_path (
    path_id     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    program_id  INT NOT NULL REFERENCES program (program_id),
    created_by  INT NOT NULL REFERENCES app_user (user_id),
    path_name   VARCHAR(150) NOT NULL,
    description VARCHAR(1000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE learning_path_course (
    path_course_id     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    path_id            INT NOT NULL REFERENCES learning_path (path_id) ON DELETE CASCADE,
    course_id          INT NOT NULL REFERENCES course (course_id),
    sequence_no        INT NOT NULL CHECK (sequence_no > 0),
    is_starting_course BOOLEAN NOT NULL DEFAULT false,
    UNIQUE (path_id, course_id),
    UNIQUE (path_id, sequence_no) DEFERRABLE INITIALLY DEFERRED   -- checked at commit, so two courses can swap places
);

CREATE TABLE enrollment (
    enrollment_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id          INT NOT NULL REFERENCES app_user (user_id),
    course_id        INT NOT NULL REFERENCES course (course_id),
    enrolled_by      INT REFERENCES app_user (user_id),
    enroll_date      TIMESTAMPTZ NOT NULL DEFAULT now(),
    status           VARCHAR(20) NOT NULL DEFAULT 'In progress' CHECK (status IN ('In progress', 'Completed', 'Dropped')),
    progress_percent NUMERIC(5,2) NOT NULL DEFAULT 0 CHECK (progress_percent BETWEEN 0 AND 100),
    completed_at     TIMESTAMPTZ,
    last_active      TIMESTAMPTZ,
    UNIQUE (user_id, course_id)
);

-- ---- Activities (a LAB row is one ordered activity of a course: an article, a lab task or the quiz) -------------------

CREATE TABLE lab (
    lab_id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id       INT NOT NULL REFERENCES course (course_id),
    title           VARCHAR(200) NOT NULL,
    article_content TEXT,
    sequence_no     INT NOT NULL CHECK (sequence_no > 0),
    is_preview      BOOLEAN NOT NULL DEFAULT false,
    status          VARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Published')),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    lesson_type     VARCHAR(10) NOT NULL DEFAULT 'article' CHECK (lesson_type IN ('article', 'lab', 'quiz')),
    minutes         INT NOT NULL DEFAULT 15,
    workspace       VARCHAR(12) NOT NULL DEFAULT 'none' CHECK (workspace IN ('none', 'javascript')),
    starter_code    TEXT
);

CREATE TABLE lab_task (
    task_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    lab_id         INT NOT NULL REFERENCES lab (lab_id) ON DELETE CASCADE,
    question_text  VARCHAR(2000) NOT NULL,
    task_type      VARCHAR(20) NOT NULL DEFAULT 'Text' CHECK (task_type IN ('Text', 'Code')),
    correct_answer VARCHAR(1000),
    points         INT NOT NULL DEFAULT 1 CHECK (points >= 0),
    sequence_no    INT NOT NULL
);

CREATE TABLE lab_submission (
    submission_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id       INT NOT NULL REFERENCES app_user (user_id),
    lab_id        INT NOT NULL REFERENCES lab (lab_id),
    score         NUMERIC(5,2),
    result_status VARCHAR(20) NOT NULL DEFAULT 'Submitted' CHECK (result_status IN ('Submitted', 'Passed', 'Failed')),
    submitted_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE task_answer (
    task_answer_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    submission_id  INT NOT NULL REFERENCES lab_submission (submission_id) ON DELETE CASCADE,
    task_id        INT NOT NULL REFERENCES lab_task (task_id),
    user_answer    TEXT,
    is_correct     BOOLEAN NOT NULL DEFAULT false,
    UNIQUE (submission_id, task_id)
);

CREATE TABLE quiz (
    quiz_id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id          INT NOT NULL REFERENCES course (course_id),
    created_by         INT NOT NULL REFERENCES app_user (user_id),
    title              VARCHAR(200) NOT NULL,
    time_limit_minutes INT CHECK (time_limit_minutes IS NULL OR time_limit_minutes > 0),
    status             VARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Published')),
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE quiz_question (
    question_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    quiz_id       INT NOT NULL REFERENCES quiz (quiz_id) ON DELETE CASCADE,
    question_text VARCHAR(1000) NOT NULL,
    question_type VARCHAR(30) NOT NULL DEFAULT 'Multiple choice' CHECK (question_type IN ('Multiple choice', 'True/False')),
    points        INT NOT NULL DEFAULT 1 CHECK (points >= 0),
    sequence_no   INT NOT NULL,
    topic         VARCHAR(100)
);

CREATE TABLE question_option (
    option_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    question_id INT NOT NULL REFERENCES quiz_question (question_id) ON DELETE CASCADE,
    option_text VARCHAR(500) NOT NULL,
    is_correct  BOOLEAN NOT NULL DEFAULT false
);

CREATE TABLE quiz_attempt (
    attempt_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    quiz_id      INT NOT NULL REFERENCES quiz (quiz_id),
    user_id      INT NOT NULL REFERENCES app_user (user_id),
    score        NUMERIC(5,2) CHECK (score IS NULL OR score BETWEEN 0 AND 100),
    started_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    submitted_at TIMESTAMPTZ,
    CHECK (submitted_at IS NULL OR submitted_at >= started_at)
);

CREATE TABLE attempt_answer (
    attempt_answer_id  INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    attempt_id         INT NOT NULL REFERENCES quiz_attempt (attempt_id) ON DELETE CASCADE,
    question_id        INT NOT NULL REFERENCES quiz_question (question_id),
    selected_option_id INT REFERENCES question_option (option_id),
    is_correct         BOOLEAN NOT NULL DEFAULT false,
    UNIQUE (attempt_id, question_id)
);

-- ---- Announcements, discussions and messages -------------------------------------------------------------------------

CREATE TABLE announcement (
    announcement_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    posted_by  INT NOT NULL REFERENCES app_user (user_id),
    course_id  INT REFERENCES course (course_id),
    program_id INT REFERENCES program (program_id),
    title      VARCHAR(200) NOT NULL,
    message    TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ
);

CREATE TABLE discussion_post (
    post_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id        INT NOT NULL REFERENCES app_user (user_id),
    course_id      INT REFERENCES course (course_id),
    lab_id         INT REFERENCES lab (lab_id),
    parent_post_id INT REFERENCES discussion_post (post_id),
    content        TEXT NOT NULL,
    status         VARCHAR(20) NOT NULL DEFAULT 'Visible' CHECK (status IN ('Visible', 'Hidden', 'Removed', 'Flagged')),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE tag (
    tag_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tag_name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE post_tag (
    post_tag_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    post_id     INT NOT NULL REFERENCES discussion_post (post_id) ON DELETE CASCADE,
    tag_id      INT NOT NULL REFERENCES tag (tag_id) ON DELETE CASCADE,
    UNIQUE (post_id, tag_id)
);

CREATE TABLE moderation_log (
    moderation_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    post_id       INT NOT NULL REFERENCES discussion_post (post_id),
    moderator_id  INT NOT NULL REFERENCES app_user (user_id),
    action        VARCHAR(30) NOT NULL,
    reason        VARCHAR(500),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE chat_message (
    message_id  INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sender_id   INT NOT NULL REFERENCES app_user (user_id),
    receiver_id INT NOT NULL REFERENCES app_user (user_id),
    content     VARCHAR(4000) NOT NULL,
    is_read     BOOLEAN NOT NULL DEFAULT false,
    sent_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    CHECK (sender_id <> receiver_id)
);

CREATE TABLE ai_chat_session (
    session_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id    INT NOT NULL REFERENCES app_user (user_id) ON DELETE CASCADE,
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    lesson_id  INT                                       -- one conversation per learner and activity; NULL is the AI Assistant page
);

CREATE TABLE ai_chat_message (
    ai_message_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    session_id    INT NOT NULL REFERENCES ai_chat_session (session_id) ON DELETE CASCADE,
    sender_type   VARCHAR(10) NOT NULL CHECK (sender_type IN ('User', 'AI')),
    content       TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_read       BOOLEAN NOT NULL DEFAULT true           -- false for an AI reply the learner has not seen yet
);

CREATE TABLE voice_channel (
    channel_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id    INT NOT NULL REFERENCES course (course_id),
    created_by   INT NOT NULL REFERENCES app_user (user_id),
    channel_name VARCHAR(100) NOT NULL,
    status       VARCHAR(20) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Closed')),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    starts_at    TIMESTAMPTZ
);

CREATE TABLE channel_participant (
    participant_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    channel_id     INT NOT NULL REFERENCES voice_channel (channel_id) ON DELETE CASCADE,
    user_id        INT NOT NULL REFERENCES app_user (user_id),
    camera_enabled BOOLEAN NOT NULL DEFAULT false,
    joined_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    left_at        TIMESTAMPTZ,
    CHECK (left_at IS NULL OR left_at >= joined_at)
);

-- ---- Feedback, enquiries and the activity log ------------------------------------------------------------------------

CREATE TABLE website_feedback (
    feedback_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES app_user (user_id),
    course_id   INT REFERENCES course (course_id),        -- set for a course review, empty for platform feedback
    rating      SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment     VARCHAR(2000),
    is_public   BOOLEAN NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE enquiry (
    enquiry_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id      INT REFERENCES app_user (user_id),
    name         VARCHAR(100) NOT NULL,
    email        VARCHAR(255) NOT NULL,
    subject      VARCHAR(200) NOT NULL,
    message      VARCHAR(4000) NOT NULL,
    status       VARCHAR(20) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'In progress', 'Resolved', 'Closed')),
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ
);

CREATE TABLE activity_log (
    log_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     INT REFERENCES app_user (user_id),
    action      VARCHAR(100) NOT NULL,
    target_type VARCHAR(50),
    target_id   INT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    detail      VARCHAR(500)
);

-- ---- Tables the web app added ----------------------------------------------------------------------------------------

-- "My enquiries": the browser keeps a random key in an HttpOnly cookie; only its SHA-256 hash is stored here.
CREATE TABLE enquiry_access (
    access_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    visitor_key_hash VARCHAR(64) NOT NULL,
    enquiry_id       INT NOT NULL REFERENCES enquiry (enquiry_id) ON DELETE CASCADE,
    granted_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (visitor_key_hash, enquiry_id)
);

-- Course files attached by staff (up to 2 MB, kept as data URLs).
CREATE TABLE course_resource (
    resource_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    course_id   INT NOT NULL REFERENCES course (course_id) ON DELETE CASCADE,
    uploaded_by INT NOT NULL REFERENCES app_user (user_id),
    file_name   VARCHAR(255) NOT NULL,
    size_label  VARCHAR(50),
    data_url    TEXT NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Create / Delete requests sent to the Program Leader. course_id, program_id and the user ids have no foreign key on
-- purpose: the history must survive an approved deletion of the course or of an account.
CREATE TABLE course_request (
    request_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    kind         VARCHAR(10) NOT NULL CHECK (kind IN ('Create', 'Delete')),
    course_id    INT NOT NULL,
    course_title VARCHAR(200) NOT NULL,
    program_id   INT,
    requester_id INT,
    status       VARCHAR(30) NOT NULL DEFAULT 'Pending review'
                 CHECK (status IN ('Pending review', 'Approved', 'Changes requested', 'Rejected', 'Withdrawn')),
    reason       VARCHAR(2000),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE course_request_event (
    event_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_id INT NOT NULL REFERENCES course_request (request_id) ON DELETE CASCADE,
    actor_id   INT,
    action     VARCHAR(30) NOT NULL
               CHECK (action IN ('Submitted', 'Review started', 'Approved', 'Changes requested', 'Rejected', 'Withdrawn')),
    comment    VARCHAR(2000),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE announcement_comment (
    comment_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    announcement_id INT NOT NULL REFERENCES announcement (announcement_id) ON DELETE CASCADE,
    user_id         INT NOT NULL REFERENCES app_user (user_id),
    content         VARCHAR(1000) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Replies to a course review (a course review is a website_feedback row with a course_id).
CREATE TABLE feedback_reply (
    reply_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    feedback_id INT NOT NULL REFERENCES website_feedback (feedback_id) ON DELETE CASCADE,
    user_id     INT NOT NULL REFERENCES app_user (user_id),
    content     VARCHAR(2000) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Which announcements each person has opened, so their badge goes away and stays away.
CREATE TABLE announcement_read (
    user_id         INT NOT NULL,
    announcement_id INT NOT NULL,
    read_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, announcement_id)
);

-- ---- The four roles. app_user.role_id points here, so the order (ids 1-4) must not change. ---------------------------

INSERT INTO role (role_name, description) VALUES
    ('Learner',        'Enrols in courses, completes activities and quizzes.'),
    ('Instructor',     'Creates and teaches courses, tracks learner performance.'),
    ('Program Leader', 'Manages programs and approves course content.'),
    ('Admin',          'Manages user accounts and the platform.');
