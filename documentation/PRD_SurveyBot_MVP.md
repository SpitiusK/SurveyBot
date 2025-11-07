# Telegram Survey Bot MVP - Product Requirements Document

## Executive Summary

A Telegram bot that enables users to create simple surveys, distribute them via Telegram, and view basic response statistics.

## MVP Scope

### In Scope
- Basic survey creation and management
- Simple question types (text, single choice, multiple choice, rating)
- Survey distribution via Telegram bot
- Response collection
- Basic statistics viewing
- Simple admin web panel

### Out of Scope
- Advanced analytics
- External integrations
- Payment features
- Advanced scheduling
- Multi-language support
- CI/CD pipelines
- Advanced reporting

## User Personas

### Survey Creator
**Goal:** Create and distribute surveys quickly via Telegram
**Needs:** Simple survey builder, easy distribution, basic results

### Survey Respondent
**Goal:** Complete surveys easily in Telegram
**Needs:** Clear questions, simple navigation, quick submission

## Functional Requirements

### 1. Survey Management

#### Create Survey
- Add survey title and description
- Set active/inactive status
- Add questions sequentially

#### Question Types
1. **Text** - Open-ended text response
2. **Single Choice** - One option from list
3. **Multiple Choice** - Multiple options from list
4. **Rating** - Scale from 1-5

### 2. Telegram Bot

#### Core Commands
- `/start` - Welcome message and instructions
- `/surveys` - List active surveys
- `/help` - Show available commands

#### Survey Flow
1. User selects survey
2. Bot presents questions one by one
3. User responds to each question
4. Bot confirms completion

### 3. Admin Panel

#### Dashboard
- Total surveys count
- Total responses count
- Active surveys list

#### Survey Builder
- Create new survey form
- Add/edit/delete questions
- Preview survey
- Activate/deactivate survey

#### Statistics View
- Response count per survey
- Question-by-question breakdown
- Simple percentage calculations for choice questions
- Export responses as CSV

## Data Model

### Core Entities

#### Survey
- ID
- Title
- Description
- IsActive
- CreatedAt

#### Question
- ID
- SurveyID
- Text
- Type
- OrderIndex
- Options (for choice questions)

#### Response
- ID
- SurveyID
- UserTelegramID
- CompletedAt

#### Answer
- ID
- ResponseID
- QuestionID
- Value

## User Stories

### Survey Creator Stories

1. **Create Survey**
   As a survey creator, I want to create a new survey with a title and description so respondents know what it's about.

2. **Add Questions**
   As a survey creator, I want to add different types of questions to my survey to gather various kinds of feedback.

3. **View Results**
   As a survey creator, I want to see how many people responded and view their answers to understand the feedback.

4. **Export Data**
   As a survey creator, I want to export responses as CSV to analyze them in Excel.

### Respondent Stories

1. **Find Surveys**
   As a respondent, I want to see available surveys in the bot so I can choose which to complete.

2. **Answer Questions**
   As a respondent, I want to answer questions one at a time with clear instructions for each type.

3. **Complete Survey**
   As a respondent, I want confirmation when I finish a survey so I know it was submitted.

## Technical Stack

### Backend
- C# .NET 8
- ASP.NET Core Web API
- PostgreSQL database
- Entity Framework Core

### Bot
- Telegram.Bot library
- Webhook-based updates

### Admin Panel
- React or Vue.js
- Simple component library (Material-UI or similar)
- Axios for API calls

## Success Metrics

### MVP Success Criteria
- Can create a survey with 4 question types
- Can distribute survey via Telegram bot
- Can collect at least 10 responses
- Can view response statistics
- Can export data to CSV

### Quality Metrics
- Survey creation takes < 5 minutes
- Bot responds within 2 seconds
- Admin panel loads within 3 seconds
- Zero data loss for responses

## Development Phases

### Phase 1: Foundation (Week 1)
- Database schema
- Basic API structure
- Entity models

### Phase 2: Core Features (Week 2)
- Survey CRUD operations
- Question management
- Basic bot commands

### Phase 3: Bot Integration (Week 3)
- Survey delivery flow
- Response collection
- Error handling

### Phase 4: Admin Panel (Week 4)
- Survey builder UI
- Statistics dashboard
- CSV export

### Phase 5: Testing & Polish (Week 5)
- End-to-end testing
- Bug fixes
- Basic documentation

## Constraints

### Technical Constraints
- Single server deployment
- Maximum 100 concurrent bot users
- Surveys limited to 20 questions

### Business Constraints
- No budget for external services
- MVP must be functional within 5 weeks
- Single developer/small team

## Definition of Done

### Feature Complete When:
- Functionality works as described
- Basic error handling implemented
- Tested with sample data
- Code reviewed and documented

### MVP Complete When:
- All user stories implemented
- End-to-end flow tested
- Deployed to production server
- Basic user documentation created

## Risk Mitigation

### Technical Risks
- **Risk:** Telegram API limits
- **Mitigation:** Implement rate limiting and queuing

### User Risks
- **Risk:** Survey abandonment
- **Mitigation:** Keep surveys short, save progress

## Future Considerations

Post-MVP features to consider:
- Advanced question types
- Conditional logic
- Multi-language support
- Analytics dashboard
- Integration APIs

---

*This PRD defines the minimum viable product for the Telegram Survey Bot. Focus on delivering these core features before considering additional functionality.*
