# SurveyBot Multimedia Support - Project Plan Summary

## Overview

This document summarizes the comprehensive task plan for adding multimedia support to the SurveyBot application. The approach focuses on extending existing question text fields with rich media content rather than creating new question types.

## Key Design Decisions

### 1. Architecture Approach

**Selected**: Extend Question entity with `MediaContent` field (JSON)

**Why**:
- Simpler data model than separate QuestionMedia table
- Cleaner 1:1 relationship within Question entity
- Easier to maintain and query
- Backward compatible (nullable field)

**Structure**:
```
Question Entity
├── QuestionText (existing)
├── QuestionType (existing - Text, SingleChoice, MultipleChoice, Rating)
└── MediaContent (NEW - JSONB storing media metadata)
```

### 2. MediaContent Schema

```json
{
  "version": "1.0",
  "items": [
    {
      "id": "uuid",
      "type": "image|video|audio|document",
      "path": "/uploads/media/2025/11/abc123.jpg",
      "displayName": "Chart.jpg",
      "mimeType": "image/jpeg",
      "sizeBytes": 245678,
      "uploadedAt": "2025-11-18T10:30:00Z",
      "altText": "Sales chart for Q4",
      "thumbnailPath": "/uploads/media/2025/11/abc123_thumb.jpg",
      "order": 0
    }
  ]
}
```

### 3. Frontend Strategy

**Rich Text Editor**: react-quill for question text editing
- Toolbar with media insert button
- Inline media preview
- Backward compatible with plain text

**Media Management**: Separate MediaGallery component
- Grid view of all media for a question
- Add/delete/reorder capabilities
- Drag-and-drop upload

## Project Breakdown

### 8 Phases, 50 Tasks, 6-7 Weeks

| Phase | Focus | Tasks | Hours | Priority |
|-------|-------|-------|-------|----------|
| **Phase 1** | Foundation & Data Model | 6 | 16 | CRITICAL |
| **Phase 2** | Backend Media API | 7 | 33 | CRITICAL |
| **Phase 3** | Frontend Rich Editor | 7 | 46 | HIGH |
| **Phase 4** | Telegram Bot Integration | 5 | 27 | HIGH |
| **Phase 5** | Testing & QA | 8 | 51 | HIGH |
| **Phase 6** | Documentation | 5 | 17 | MEDIUM |
| **Phase 7** | Deployment & Optimization | 6 | 27 | MEDIUM |
| **Phase 8** | Final Integration & Release | 6 | 47 | HIGH |

**Total Effort**: 264 hours (~33 days, ~7 weeks)

## Critical Path

The following tasks form the critical path and must be completed sequentially:

1. **TASK-MM-001**: Design MediaContent JSON schema (3h)
2. **TASK-MM-002**: Extend Question entity (2h)
3. **TASK-MM-004**: Configure JSONB column (2h)
4. **TASK-MM-005**: Generate database migration (2h)
5. **TASK-MM-007**: Create IMediaStorageService (2h)
6. **TASK-MM-008**: Implement FileSystemMediaStorageService (8h)
7. **TASK-MM-010**: Create MediaController with upload (6h)
8. **TASK-MM-014**: Install React packages (1h)
9. **TASK-MM-016**: Create RichTextEditor component (10h)
10. **TASK-MM-018**: Update QuestionForm (5h)
11. **TASK-MM-045**: Integration testing (8h)
12. **TASK-MM-047**: Bug fixes (16h)
13. **TASK-MM-049**: Production deployment (4h)

**Critical Path Total**: ~69 hours (~9 days)

## Parallel Work Opportunities

Several task groups can be executed in parallel:

### Group 1 (During Phase 1)
- TASK-MM-003: Create MediaItem DTOs
- TASK-MM-006: Create MediaContentHelper utility

### Group 2 (During Phase 2)
- TASK-MM-009: MediaValidationService
- TASK-MM-015: MediaPicker component (frontend)

### Group 3 (During Phase 3)
- TASK-MM-017: MediaGallery component
- TASK-MM-021: TelegramMediaService

### Group 4 (During Phase 5)
- TASK-MM-026: Unit tests for helper
- TASK-MM-027: Unit tests for storage
- TASK-MM-028: Unit tests for validation

## Key Technologies

### Backend
- **Database**: PostgreSQL JSONB for MediaContent
- **Storage**: File system (wwwroot/uploads/media)
- **Image Processing**: ImageSharp (compression, thumbnails)
- **Validation**: Custom MediaValidationService

### Frontend
- **Rich Text Editor**: react-quill 2.0
- **File Upload**: react-dropzone 14.0
- **Media Preview**: Custom components
- **HTTP Client**: Axios (existing)

### Telegram Bot
- **Media Sending**: Telegram.Bot library methods
- **File Download**: Telegram GetFileAsync API
- **State Management**: Existing conversation state

## Configuration Example

```json
{
  "MediaSettings": {
    "AllowedTypes": {
      "Image": ["jpg", "jpeg", "png", "gif", "webp"],
      "Video": ["mp4", "webm", "mov"],
      "Audio": ["mp3", "wav", "ogg", "m4a"],
      "Document": ["pdf", "doc", "docx", "txt"]
    },
    "MaxFileSizeMB": {
      "Image": 10,
      "Video": 50,
      "Audio": 20,
      "Document": 25
    },
    "StoragePath": "wwwroot/uploads/media",
    "UrlPattern": "/uploads/media/{year}/{month}/{filename}",
    "EnableCompression": true,
    "GenerateThumbnails": true
  }
}
```

## Database Migration

### Before Migration
```sql
-- Questions table structure
CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    survey_id INTEGER NOT NULL,
    question_text TEXT NOT NULL,
    question_type VARCHAR(20) NOT NULL,
    order_index INTEGER NOT NULL,
    is_required BOOLEAN NOT NULL,
    options_json JSONB,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);
```

### After Migration
```sql
-- Added column
ALTER TABLE questions ADD COLUMN media_content JSONB;

-- Added index
CREATE INDEX ix_questions_media_content ON questions USING gin(media_content);
```

**Impact**:
- No downtime required (nullable column)
- Existing questions unaffected
- Instant migration (no data transformation)

## Risk Mitigation

### High-Priority Risks

**Risk 1**: Backward Compatibility
- **Mitigation**: MediaContent is nullable, thorough testing, default values
- **Owner**: All agents

**Risk 2**: Security Vulnerabilities
- **Mitigation**: Comprehensive validation, security testing, file type checking
- **Owner**: Backend agent

**Risk 3**: Database Migration Failures
- **Mitigation**: Test migrations, backup procedures, rollback plan
- **Owner**: Database agent

### Medium-Priority Risks

**Risk 4**: Disk Space Consumption
- **Mitigation**: File size limits, compression, cleanup job
- **Owner**: Backend agent

**Risk 5**: Telegram API Rate Limits
- **Mitigation**: Respect limits, retries, graceful degradation
- **Owner**: Bot agent

## Success Metrics

### Functionality
- Survey creators can add images, videos, audio, documents to questions
- Multiple media per question supported
- Media displays correctly in web and Telegram
- Existing plain text questions work unchanged

### Performance
- Media upload: <10s for files under 10MB
- Page load increase: <200ms with media
- API response increase: <100ms for media questions

### Quality
- Unit test coverage: >80%
- All integration and E2E tests pass
- No critical/high bugs in production
- User acceptance: >80% satisfaction

### Security
- Authentication required for uploads
- User can only delete own media
- File type validation prevents malicious uploads
- No XSS vulnerabilities

## Deliverables

### Code
1. Extended Question entity with MediaContent field
2. Database migration for MediaContent column
3. MediaStorageService and MediaValidationService
4. MediaController with upload/delete endpoints
5. RichTextEditor and MediaPicker React components
6. Updated QuestionForm with media support
7. TelegramMediaService for bot integration
8. Comprehensive test suite (unit, integration, E2E)

### Documentation
1. API documentation (Swagger)
2. Developer guide for multimedia feature
3. User guide (web and bot)
4. Database migration guide
5. Configuration examples
6. Release notes and changelog

### Infrastructure
1. Production media storage setup
2. Image optimization pipeline
3. Media cleanup background job
4. Monitoring and logging
5. Deployment scripts

## Timeline

### Week 1: Foundation
- Design schema
- Extend entity and create migration
- Setup utilities and helpers

### Week 2: Backend API
- Implement storage services
- Create upload/delete endpoints
- Add validation

### Week 3-4: Frontend
- Install packages and create components
- Build RichTextEditor with media support
- Update survey builder UI

### Week 4-5: Bot & Testing
- Integrate Telegram bot media support
- Comprehensive testing (unit, integration, E2E)
- Security and performance testing

### Week 6: Documentation & Deployment
- Complete all documentation
- Deploy to production
- Post-release monitoring

### Week 6+: Optimization & Support
- Performance optimization
- Bug fixes
- User support

## Next Steps

1. **Review and approve** this task plan
2. **Setup project** tracking (Jira, GitHub Projects, etc.)
3. **Assign resources** to task groups
4. **Begin Phase 1** with schema design and entity extension
5. **Schedule daily standups** to track progress
6. **Plan sprint boundaries** (recommend 2-week sprints)

## Questions for Stakeholders

1. **Storage**: Start with file system or plan for cloud storage (S3, Azure Blob)?
2. **CDN**: Immediate CDN integration or future enhancement?
3. **Virus Scanning**: Required for uploaded files?
4. **Media Retention**: How long to keep orphaned media files?
5. **User Quotas**: Per-user storage limits needed?
6. **Admin Tools**: Media management dashboard for admins?

## Conclusion

This plan provides a comprehensive, phased approach to adding multimedia support to SurveyBot. By extending the existing Question entity rather than creating new structures, we minimize complexity while maximizing backward compatibility. The 50 tasks across 8 phases provide clear, actionable steps with defined acceptance criteria, ensuring successful delivery of this significant enhancement.

**Estimated Timeline**: 6-7 weeks
**Total Effort**: 264 hours
**Team Size**: 5-7 developers (frontend, backend, bot, testing, DevOps)

---

**Document Version**: 1.0
**Created**: 2025-11-18
**Author**: Project Manager Agent
**Status**: Ready for Review
