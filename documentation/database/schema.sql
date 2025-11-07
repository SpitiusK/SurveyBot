-- ============================================================================
-- Telegram Survey Bot - PostgreSQL Database Schema
-- Version: 1.0.0 (MVP)
-- Description: Complete database schema with entities, relationships, and indexes
-- ============================================================================

-- Enable UUID extension for generating unique identifiers
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- TABLE: users
-- Description: Stores Telegram user information
-- ============================================================================
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    telegram_id BIGINT NOT NULL UNIQUE,
    username VARCHAR(255),
    first_name VARCHAR(255),
    last_name VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index for fast telegram_id lookups
CREATE INDEX idx_users_telegram_id ON users(telegram_id);

-- Index for username searches
CREATE INDEX idx_users_username ON users(username) WHERE username IS NOT NULL;

-- ============================================================================
-- TABLE: surveys
-- Description: Stores survey metadata and configuration
-- ============================================================================
CREATE TABLE surveys (
    id SERIAL PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    creator_id INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    allow_multiple_responses BOOLEAN NOT NULL DEFAULT false,
    show_results BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Foreign key constraint
    CONSTRAINT fk_surveys_creator
        FOREIGN KEY (creator_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);

-- Index for creator lookups (find all surveys by a user)
CREATE INDEX idx_surveys_creator_id ON surveys(creator_id);

-- Index for active survey queries
CREATE INDEX idx_surveys_is_active ON surveys(is_active) WHERE is_active = true;

-- Composite index for common query pattern (creator + active status)
CREATE INDEX idx_surveys_creator_active ON surveys(creator_id, is_active);

-- Index for sorting by creation date
CREATE INDEX idx_surveys_created_at ON surveys(created_at DESC);

-- ============================================================================
-- TABLE: questions
-- Description: Stores survey questions with type and configuration
-- ============================================================================
CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    survey_id INTEGER NOT NULL,
    question_text TEXT NOT NULL,
    question_type VARCHAR(50) NOT NULL,
    order_index INTEGER NOT NULL,
    is_required BOOLEAN NOT NULL DEFAULT true,
    options_json JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Foreign key constraint
    CONSTRAINT fk_questions_survey
        FOREIGN KEY (survey_id)
        REFERENCES surveys(id)
        ON DELETE CASCADE,

    -- Check constraints
    CONSTRAINT chk_question_type
        CHECK (question_type IN ('text', 'multiple_choice', 'single_choice', 'rating', 'yes_no')),
    CONSTRAINT chk_order_index
        CHECK (order_index >= 0)
);

-- Index for fetching all questions for a survey
CREATE INDEX idx_questions_survey_id ON questions(survey_id);

-- Composite index for ordered question retrieval
CREATE INDEX idx_questions_survey_order ON questions(survey_id, order_index);

-- Index for question type filtering
CREATE INDEX idx_questions_type ON questions(question_type);

-- GIN index for JSONB options searching
CREATE INDEX idx_questions_options_json ON questions USING GIN (options_json);

-- Unique constraint to prevent duplicate order within a survey
CREATE UNIQUE INDEX idx_questions_survey_order_unique ON questions(survey_id, order_index);

-- ============================================================================
-- TABLE: responses
-- Description: Stores user responses to surveys
-- ============================================================================
CREATE TABLE responses (
    id SERIAL PRIMARY KEY,
    survey_id INTEGER NOT NULL,
    respondent_telegram_id BIGINT NOT NULL,
    is_complete BOOLEAN NOT NULL DEFAULT false,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP WITH TIME ZONE,

    -- Foreign key constraints
    CONSTRAINT fk_responses_survey
        FOREIGN KEY (survey_id)
        REFERENCES surveys(id)
        ON DELETE CASCADE,

    -- Check constraint
    CONSTRAINT chk_submitted_at
        CHECK (submitted_at IS NULL OR submitted_at >= started_at)
);

-- Index for finding responses by survey
CREATE INDEX idx_responses_survey_id ON responses(survey_id);

-- Index for finding responses by respondent
CREATE INDEX idx_responses_respondent ON responses(respondent_telegram_id);

-- Composite index for finding user's responses to a specific survey
CREATE INDEX idx_responses_survey_respondent ON responses(survey_id, respondent_telegram_id);

-- Index for complete responses only
CREATE INDEX idx_responses_complete ON responses(is_complete) WHERE is_complete = true;

-- Index for sorting by submission date
CREATE INDEX idx_responses_submitted_at ON responses(submitted_at DESC) WHERE submitted_at IS NOT NULL;

-- ============================================================================
-- TABLE: answers
-- Description: Stores individual answers to questions
-- ============================================================================
CREATE TABLE answers (
    id SERIAL PRIMARY KEY,
    response_id INTEGER NOT NULL,
    question_id INTEGER NOT NULL,
    answer_text TEXT,
    answer_json JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Foreign key constraints
    CONSTRAINT fk_answers_response
        FOREIGN KEY (response_id)
        REFERENCES responses(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_answers_question
        FOREIGN KEY (question_id)
        REFERENCES questions(id)
        ON DELETE CASCADE,

    -- Check constraint to ensure at least one answer field is provided
    CONSTRAINT chk_answer_not_null
        CHECK (answer_text IS NOT NULL OR answer_json IS NOT NULL)
);

-- Index for fetching all answers for a response
CREATE INDEX idx_answers_response_id ON answers(response_id);

-- Index for fetching answers by question (for analytics)
CREATE INDEX idx_answers_question_id ON answers(question_id);

-- Composite index for finding specific answer
CREATE INDEX idx_answers_response_question ON answers(response_id, question_id);

-- GIN index for JSONB answer searching and analytics
CREATE INDEX idx_answers_answer_json ON answers USING GIN (answer_json);

-- Unique constraint to prevent duplicate answers for the same question in a response
CREATE UNIQUE INDEX idx_answers_response_question_unique ON answers(response_id, question_id);

-- ============================================================================
-- TRIGGERS: Automatic timestamp updates
-- ============================================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for users table
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for surveys table
CREATE TRIGGER trg_surveys_updated_at
    BEFORE UPDATE ON surveys
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- VIEWS: Useful views for common queries
-- ============================================================================

-- View: Active surveys with creator information
CREATE OR REPLACE VIEW v_active_surveys AS
SELECT
    s.id,
    s.title,
    s.description,
    s.creator_id,
    u.telegram_id as creator_telegram_id,
    u.username as creator_username,
    s.allow_multiple_responses,
    s.show_results,
    s.created_at,
    s.updated_at,
    COUNT(DISTINCT q.id) as question_count,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete = true) as response_count
FROM surveys s
LEFT JOIN users u ON s.creator_id = u.id
LEFT JOIN questions q ON s.id = q.survey_id
LEFT JOIN responses r ON s.id = r.survey_id
WHERE s.is_active = true
GROUP BY s.id, u.telegram_id, u.username;

-- View: Survey statistics
CREATE OR REPLACE VIEW v_survey_statistics AS
SELECT
    s.id as survey_id,
    s.title,
    COUNT(DISTINCT q.id) as total_questions,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete = true) as completed_responses,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete = false) as incomplete_responses,
    COUNT(DISTINCT r.respondent_telegram_id) as unique_respondents,
    MIN(r.submitted_at) as first_response_date,
    MAX(r.submitted_at) as last_response_date
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
LEFT JOIN responses r ON s.id = r.survey_id
GROUP BY s.id, s.title;

-- ============================================================================
-- SEED DATA: Initial data for development/testing
-- ============================================================================

-- Note: This section can be used to insert sample data for development
-- Uncomment and modify as needed

-- ============================================================================
-- COMMENTS: Table and column documentation
-- ============================================================================

COMMENT ON TABLE users IS 'Stores Telegram user information';
COMMENT ON COLUMN users.telegram_id IS 'Unique Telegram user ID from Telegram API';
COMMENT ON COLUMN users.username IS 'Telegram username (without @)';

COMMENT ON TABLE surveys IS 'Stores survey metadata and configuration';
COMMENT ON COLUMN surveys.allow_multiple_responses IS 'Whether a user can submit multiple responses to this survey';
COMMENT ON COLUMN surveys.show_results IS 'Whether to show results to respondents after submission';

COMMENT ON TABLE questions IS 'Stores survey questions with type and configuration';
COMMENT ON COLUMN questions.question_type IS 'Type of question: text, multiple_choice, single_choice, rating, yes_no';
COMMENT ON COLUMN questions.order_index IS 'Display order of question in survey (0-based)';
COMMENT ON COLUMN questions.options_json IS 'JSON array of options for choice-type questions';

COMMENT ON TABLE responses IS 'Stores user responses to surveys';
COMMENT ON COLUMN responses.is_complete IS 'Whether the response has been fully submitted';
COMMENT ON COLUMN responses.started_at IS 'When the user started the survey';
COMMENT ON COLUMN responses.submitted_at IS 'When the user submitted the completed survey';

COMMENT ON TABLE answers IS 'Stores individual answers to questions';
COMMENT ON COLUMN answers.answer_text IS 'Text answer for simple text questions';
COMMENT ON COLUMN answers.answer_json IS 'JSON answer for complex questions (multiple choice, ratings, etc.)';

-- ============================================================================
-- END OF SCHEMA
-- ============================================================================
