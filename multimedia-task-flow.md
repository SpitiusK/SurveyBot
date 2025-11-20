# SurveyBot Multimedia Support - Task Flow Diagram

## Critical Path Visualization

```
PHASE 1: Foundation & Data Model
┌─────────────────────────────────────────────────────────────┐
│  TASK-MM-001: Design MediaContent JSON schema (3h)         │
│                          ↓                                   │
│  TASK-MM-002: Extend Question entity (2h)                  │
│                          ↓                                   │
│  TASK-MM-004: Configure JSONB column (2h)                  │
│                          ↓                                   │
│  TASK-MM-005: Generate database migration (2h)             │
└─────────────────────────────────────────────────────────────┘
                           ↓

PHASE 2: Backend Media API
┌─────────────────────────────────────────────────────────────┐
│  TASK-MM-007: Create IMediaStorageService (2h)             │
│                          ↓                                   │
│  TASK-MM-008: Implement FileSystemMediaStorageService (8h) │
│                          ↓                                   │
│  TASK-MM-010: Create MediaController with upload (6h)      │
└─────────────────────────────────────────────────────────────┘
                           ↓

PHASE 3: Frontend Rich Text Editor
┌─────────────────────────────────────────────────────────────┐
│  TASK-MM-014: Install react-quill and dependencies (1h)    │
│                          ↓                                   │
│  TASK-MM-016: Create RichTextEditor component (10h)        │
│                          ↓                                   │
│  TASK-MM-018: Update QuestionForm (5h)                     │
└─────────────────────────────────────────────────────────────┘
                           ↓

PHASE 5: Testing & QA
┌─────────────────────────────────────────────────────────────┐
│  TASK-MM-045: Integration testing (8h)                     │
│                          ↓                                   │
│  TASK-MM-047: Bug fixes (16h)                              │
└─────────────────────────────────────────────────────────────┘
                           ↓

PHASE 8: Final Integration & Release
┌─────────────────────────────────────────────────────────────┐
│  TASK-MM-049: Production deployment (4h)                   │
└─────────────────────────────────────────────────────────────┘

CRITICAL PATH TOTAL: ~69 hours (~9 days)
```

## Parallel Execution Opportunities

### Week 1-2: Foundation + Backend

```
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-001       │  │   TASK-MM-003       │  │   TASK-MM-006       │
│   Design Schema     │  │   MediaItem DTOs    │  │   Helper Utility    │
│   (Critical Path)   │  │   (Parallel)        │  │   (Parallel)        │
│        3h           │  │        3h           │  │        4h           │
└──────────┬──────────┘  └─────────────────────┘  └─────────────────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-002       │
│   Extend Entity     │
│   (Critical Path)   │
│        2h           │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-004       │
│   Configure JSONB   │
│   (Critical Path)   │
│        2h           │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-005       │
│   DB Migration      │
│   (Critical Path)   │
│        2h           │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-007       │  │   TASK-MM-009       │
│   Storage Interface │  │   Validation Svc    │
│   (Critical Path)   │  │   (Parallel)        │
│        2h           │  │        5h           │
└──────────┬──────────┘  └─────────────────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-008       │
│   Storage Service   │
│   (Critical Path)   │
│        8h           │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-010       │
│   Upload Endpoint   │
│   (Critical Path)   │
│        6h           │
└─────────────────────┘
```

### Week 2-3: Frontend Development

```
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-014       │  │   TASK-MM-015       │
│   Install Packages  │  │   MediaPicker       │
│   (Critical Path)   │  │   (Parallel)        │
│        1h           │  │        8h           │
└──────────┬──────────┘  └─────────────────────┘
           │
           ↓
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-016       │  │   TASK-MM-017       │
│   RichTextEditor    │  │   MediaGallery      │
│   (Critical Path)   │  │   (Parallel)        │
│        10h          │  │        6h           │
└──────────┬──────────┘  └─────────────────────┘
           │
           ↓
┌─────────────────────┐
│   TASK-MM-018       │
│   Update QForm      │
│   (Critical Path)   │
│        5h           │
└─────────────────────┘
```

### Week 3-4: Bot Integration (Parallel with Testing)

```
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-021       │  │   TASK-MM-026       │
│   TelegramMedia Svc │  │   Test Helper       │
│        4h           │  │   (Parallel)        │
└──────────┬──────────┘  │        4h           │
           │             └─────────────────────┘
           ↓
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-022       │  │   TASK-MM-027       │
│   Question Handler  │  │   Test Storage      │
│        6h           │  │   (Parallel)        │
└──────────┬──────────┘  │        6h           │
           │             └─────────────────────┘
           ↓
┌─────────────────────┐  ┌─────────────────────┐
│   TASK-MM-023       │  │   TASK-MM-028       │
│   Preview Command   │  │   Test Validation   │
│        3h           │  │   (Parallel)        │
└─────────────────────┘  │        5h           │
                         └─────────────────────┘
```

## Phase Dependencies

```
PHASE 1: Foundation
       ↓
PHASE 2: Backend API
       ↓
   ┌───┴───┐
   ↓       ↓
PHASE 3  PHASE 4
Frontend   Bot
   ↓       ↓
   └───┬───┘
       ↓
PHASE 5: Testing
       ↓
PHASE 6: Documentation (can start earlier)
       ↓
PHASE 7: Deployment & Optimization
       ↓
PHASE 8: Final Integration
```

## Resource Allocation by Week

### Week 1: Foundation & Backend Start
- **Database Agent**: 14 hours (MM-001 to MM-006)
- **Backend Agent**: 10 hours (MM-007 to MM-008)

### Week 2: Backend API Complete
- **Backend Agent**: 23 hours (MM-009 to MM-013)

### Week 3: Frontend Development
- **Frontend Agent**: 24 hours (MM-014 to MM-017)

### Week 4: Frontend Integration + Bot
- **Frontend Agent**: 22 hours (MM-018 to MM-020)
- **Bot Agent**: 13 hours (MM-021 to MM-023)

### Week 5: Testing
- **Testing Agent**: 35 hours (MM-026 to MM-033)
- **Bot Agent**: 14 hours (MM-024 to MM-025)

### Week 6: Documentation + Deployment
- **Project Manager**: 13 hours (MM-035 to MM-038)
- **Backend Agent**: 13 hours (MM-034, MM-040 to MM-042)
- **DevOps Agent**: 8 hours (MM-039, MM-049)

### Week 6+: Final Integration
- **All Agents**: 24 hours (MM-045 to MM-048, MM-050)

## Sprint Recommendations

### Sprint 1 (2 weeks): Foundation + Backend
**Goals**:
- Complete Phase 1 (Foundation)
- Complete Phase 2 (Backend API)
- Start Phase 3 (Frontend)

**Deliverables**:
- Database migration ready
- Media upload/delete API working
- React packages installed

**Team**: Database Agent, Backend Agent, Frontend Agent (part-time)

---

### Sprint 2 (2 weeks): Frontend + Bot
**Goals**:
- Complete Phase 3 (Frontend)
- Complete Phase 4 (Bot)
- Start Phase 5 (Testing)

**Deliverables**:
- RichTextEditor working
- QuestionForm updated
- Bot can send media
- Unit tests started

**Team**: Frontend Agent, Bot Agent, Testing Agent (part-time)

---

### Sprint 3 (2 weeks): Testing + Documentation + Deploy
**Goals**:
- Complete Phase 5 (Testing)
- Complete Phase 6 (Documentation)
- Complete Phase 7 (Deployment)
- Complete Phase 8 (Release)

**Deliverables**:
- All tests passing
- Documentation complete
- Deployed to production
- Post-release monitoring

**Team**: Testing Agent, Project Manager, DevOps Agent, All Agents (bug fixes)

---

## Daily Standup Topics

### Week 1-2 (Backend Focus)
- Database migration status
- API endpoint progress
- Storage service implementation
- Any blocking issues with file handling

### Week 3-4 (Frontend/Bot Focus)
- React component development
- Media picker functionality
- Bot integration challenges
- Cross-browser testing

### Week 5-6 (Testing/Deploy Focus)
- Test coverage metrics
- Bug triage and prioritization
- Documentation completion
- Deployment readiness

## Key Milestones

1. **Week 1 End**: Database migration applied, schema validated
2. **Week 2 End**: Media upload API working, validated
3. **Week 3 End**: RichTextEditor integrated in admin panel
4. **Week 4 End**: Complete workflow (create with media, take survey) working
5. **Week 5 End**: All tests passing, bugs triaged
6. **Week 6 End**: Production deployment complete

## Blockers to Watch

1. **Database Migration**: Must complete before any other work
2. **File Storage**: File system permissions critical
3. **React Integration**: May need package version compatibility fixes
4. **Telegram API**: Rate limits may require throttling
5. **Testing**: May discover unexpected edge cases

## Success Checkpoints

- [ ] Database migration runs without errors
- [ ] Can upload image via API
- [ ] Can see uploaded image in admin panel
- [ ] Can create question with image in RichTextEditor
- [ ] Can see image in survey preview
- [ ] Can see image when taking survey (web)
- [ ] Can see image when taking survey (Telegram bot)
- [ ] Can delete media
- [ ] Existing plain text questions still work
- [ ] All tests pass
- [ ] Documentation complete
- [ ] Deployed to production

---

**Total Timeline**: 6-7 weeks
**Team Size**: 5-7 developers
**Estimated Effort**: 264 hours

This flow ensures minimal blocking and maximum parallel execution while maintaining dependency integrity.
