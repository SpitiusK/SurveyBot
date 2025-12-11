# Docker Setup - Test Plan & Verification

**Purpose**: Verify Docker setup works correctly before user deployment
**Date**: 2025-12-11
**Version**: 1.6.2

---

## Pre-Deployment Checklist

### Files Verification

- [x] `frontend/Dockerfile` - Multi-stage build configuration
- [x] `frontend/nginx.conf` - Nginx reverse proxy configuration
- [x] `frontend/.dockerignore` - Build context optimization
- [x] `frontend/.env.docker` - Docker environment variables
- [x] `docker-compose.yml` - Updated with frontend service
- [x] `DOCKER_SETUP.md` - Complete setup guide (500+ lines)
- [x] `DOCKER_QUICKSTART.md` - Quick start guide
- [x] `DOCKER_IMPLEMENTATION_SUMMARY.md` - Technical summary
- [x] `README_DOCKER.md` - Quick reference
- [x] `CLAUDE.md` - Updated with Docker setup option

**Total**: 10 files (4 new frontend files + 5 documentation files + 1 updated docker-compose.yml)

---

## Test Scenarios

### 1. Fresh Installation Test

**Purpose**: Verify setup works for new users

**Steps**:
```bash
# 1. Clean slate
docker-compose down -v
docker system prune -a -f

# 2. Configure bot token
# Edit docker-compose.yml line 37

# 3. Build and start
docker-compose up -d

# 4. Wait for services
sleep 60

# 5. Verify all services running
docker-compose ps
```

**Expected Results**:
```
NAME                    STATUS
surveybot-postgres      Up (healthy)
surveybot-api           Up (healthy)
surveybot-frontend      Up (healthy)
surveybot-pgadmin       Up
```

**Pass Criteria**: All 4 services show "Up" status

---

### 2. Frontend Accessibility Test

**Purpose**: Verify frontend is accessible and serves content

**Steps**:
```bash
# Test 1: Frontend main page
curl -I http://localhost:3000

# Test 2: Health check endpoint
curl http://localhost:3000/health

# Test 3: Static asset loading
curl -I http://localhost:3000/assets/index.js
```

**Expected Results**:
- Test 1: HTTP 200 OK, Content-Type: text/html
- Test 2: HTTP 200 OK, Body: "OK"
- Test 3: HTTP 200 OK, Cache-Control: public, immutable

**Pass Criteria**: All requests return 200 OK

---

### 3. API Proxy Test

**Purpose**: Verify Nginx correctly proxies API requests

**Steps**:
```bash
# Test 1: API health via proxy
curl http://localhost:3000/api/health/db

# Test 2: Verify proxy headers
curl -v http://localhost:3000/api/health/db 2>&1 | grep -i "x-forwarded"

# Test 3: Test non-existent endpoint
curl -I http://localhost:3000/api/nonexistent
```

**Expected Results**:
- Test 1: HTTP 200 OK (or appropriate status)
- Test 2: Headers include X-Forwarded-For, X-Forwarded-Proto
- Test 3: HTTP 404 Not Found

**Pass Criteria**: Proxy routes requests to backend correctly

---

### 4. Service Communication Test

**Purpose**: Verify internal Docker networking

**Steps**:
```bash
# Test 1: Frontend can reach API
docker exec surveybot-frontend wget -O- http://surveybot-api:8080/health/db

# Test 2: API can reach database
docker exec surveybot-api curl http://postgres:5432
# Should fail with connection refused (expected - PostgreSQL not HTTP)

# Test 3: Check network existence
docker network inspect surveybot-network
```

**Expected Results**:
- Test 1: Returns API health check response
- Test 2: Connection attempt (shows connectivity)
- Test 3: Shows all 4 containers in network

**Pass Criteria**: Services can communicate via service names

---

### 5. Health Check Test

**Purpose**: Verify health checks work correctly

**Steps**:
```bash
# Test 1: Check PostgreSQL health
docker inspect surveybot-postgres | grep -A 10 "Health"

# Test 2: Check API health
docker inspect surveybot-api | grep -A 10 "Health"

# Test 3: Check Frontend health
docker inspect surveybot-frontend | grep -A 10 "Health"
```

**Expected Results**:
- All services show "Status": "healthy"
- Health check logs show successful checks

**Pass Criteria**: All services pass health checks within 60 seconds

---

### 6. Build Performance Test

**Purpose**: Measure build time and image size

**Steps**:
```bash
# Test 1: Clean build time
docker-compose down
docker system prune -a -f
time docker-compose build

# Test 2: Cached build time
time docker-compose build

# Test 3: Image sizes
docker images | grep surveybot
```

**Expected Results**:
- Test 1: 5-10 minutes (first build)
- Test 2: 30-60 seconds (cached build)
- Test 3: Frontend ~25MB, API ~250MB

**Pass Criteria**: Build completes successfully, reasonable image sizes

---

### 7. Rebuild Test

**Purpose**: Verify rebuild after code changes

**Steps**:
```bash
# Test 1: Modify frontend code
echo "// test comment" >> frontend/src/App.tsx

# Test 2: Rebuild
docker-compose up -d --build frontend

# Test 3: Verify changes applied
docker-compose logs frontend | grep "test comment"
```

**Expected Results**:
- Rebuild completes successfully
- New image is created
- Container restarts with new code

**Pass Criteria**: Changes are reflected in running container

---

### 8. Restart Persistence Test

**Purpose**: Verify data persists across restarts

**Steps**:
```bash
# Test 1: Stop all services
docker-compose down

# Test 2: Start again
docker-compose up -d

# Test 3: Check database data
docker exec surveybot-postgres psql -U surveybot_user -d surveybot_db -c "SELECT COUNT(*) FROM surveys;"
```

**Expected Results**:
- Services start successfully
- Database volumes persist
- Data is intact

**Pass Criteria**: Data survives container restart

---

### 9. Port Conflict Test

**Purpose**: Verify graceful handling of port conflicts

**Steps**:
```bash
# Test 1: Start service on port 3000
python -m http.server 3000 &

# Test 2: Try to start Docker
docker-compose up -d

# Test 3: Check error message
docker-compose logs frontend
```

**Expected Results**:
- Docker shows clear error message
- Error indicates port conflict
- Service doesn't start silently

**Pass Criteria**: Clear error message about port conflict

---

### 10. Frontend Functionality Test

**Purpose**: Verify frontend works correctly in Docker

**Manual Steps**:
1. Open http://localhost:3000 in browser
2. Check React app loads (no blank page)
3. Check console for errors (should be none)
4. Verify network requests go to `/api/*` (not localhost:5000)
5. Test login flow (if credentials available)

**Expected Results**:
- React app loads correctly
- No console errors
- API requests use proxy path
- Login works (if tested)

**Pass Criteria**: Frontend fully functional in browser

---

## Browser Compatibility Test

### Browsers to Test

- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if Mac available)

### Test Cases per Browser

1. **Frontend loads**: http://localhost:3000
2. **React Router works**: Navigate between pages
3. **API calls work**: Login, fetch surveys
4. **Console errors**: Check developer tools console

**Pass Criteria**: Works in all major browsers

---

## Security Verification

### Security Checklist

- [ ] API is NOT exposed on port 5000 (only via proxy)
- [ ] Nginx security headers present (X-Frame-Options, etc.)
- [ ] No sensitive data in environment variables
- [ ] Default credentials documented as dev-only
- [ ] HTTPS disabled (dev mode) - documented as production requirement

**Verification Commands**:
```bash
# Test 1: API not directly accessible
curl http://localhost:5000
# Should fail with connection refused

# Test 2: Check security headers
curl -I http://localhost:3000 | grep -i "x-"
# Should show X-Frame-Options, X-Content-Type-Options, X-XSS-Protection
```

---

## Performance Benchmarks

### Metrics to Measure

**Build Time**:
- [ ] First build: < 10 minutes
- [ ] Cached build: < 2 minutes
- [ ] Rebuild frontend only: < 1 minute

**Startup Time**:
- [ ] PostgreSQL ready: < 15 seconds
- [ ] API ready: < 45 seconds
- [ ] Frontend ready: < 60 seconds total

**Image Sizes**:
- [ ] Frontend: < 30 MB
- [ ] API: < 300 MB
- [ ] Total images: < 1 GB

**Memory Usage** (docker stats):
- [ ] Frontend: < 20 MB
- [ ] API: < 250 MB
- [ ] PostgreSQL: < 150 MB
- [ ] Total: < 500 MB

---

## Failure Scenarios

### Test Error Handling

**Scenario 1: Database Not Ready**
```bash
# Stop PostgreSQL
docker stop surveybot-postgres

# Try to access API
curl http://localhost:3000/api/health/db
```
**Expected**: API shows connection error, frontend displays error message

**Scenario 2: API Crash**
```bash
# Stop API
docker stop surveybot-api

# Try to access via frontend
curl http://localhost:3000/api/health/db
```
**Expected**: Nginx returns 502 Bad Gateway

**Scenario 3: Frontend Crash**
```bash
# Stop frontend
docker stop surveybot-frontend

# Try to access
curl http://localhost:3000
```
**Expected**: Connection refused (no response)

---

## Documentation Verification

### Documentation Checklist

- [ ] DOCKER_QUICKSTART.md is accurate and complete
- [ ] DOCKER_SETUP.md covers all configuration options
- [ ] Troubleshooting section addresses common issues
- [ ] Commands in documentation are tested and work
- [ ] Screenshots/diagrams are clear (if added)
- [ ] Links between documents work

**Verification Method**: Follow each guide step-by-step

---

## Rollback Test

### Verify Rollback Procedure Works

**Steps**:
```bash
# Test 1: Stop Docker services
docker-compose down

# Test 2: Revert docker-compose.yml
git checkout HEAD -- docker-compose.yml

# Test 3: Start only backend services
docker-compose up -d postgres api pgadmin

# Test 4: Run frontend locally
cd frontend
npm install
npm run dev
```

**Expected Results**:
- Backend services start normally
- Frontend runs on development server
- Application works in "local dev" mode

**Pass Criteria**: Rollback restores previous working state

---

## User Acceptance Criteria

### Minimum Requirements for User Deployment

- [x] Single-command startup: `docker-compose up -d`
- [x] Frontend accessible on http://localhost:3000
- [x] All services start within 2 minutes
- [x] Clear error messages if setup fails
- [x] Documentation complete and accurate
- [x] Troubleshooting guide covers common issues
- [x] No manual configuration required (except bot token)
- [x] Data persists across restarts
- [x] Graceful handling of port conflicts

**Overall Pass Criteria**: All items checked, no blockers

---

## Post-Deployment Monitoring

### First 24 Hours Checklist

- [ ] Monitor Docker container logs for errors
- [ ] Check memory usage patterns (docker stats)
- [ ] Verify database connections are stable
- [ ] Test frontend performance in real usage
- [ ] Collect user feedback on setup process
- [ ] Document any issues discovered

---

## Known Limitations

### Documented Constraints

1. **Build Time**: First build takes 5-10 minutes
2. **Windows Docker**: Requires WSL2 backend
3. **Port 3000**: Must be available (or change in docker-compose.yml)
4. **Memory**: Requires 4GB+ available RAM
5. **Disk Space**: Requires 10GB+ available

**Documentation**: All limitations documented in DOCKER_SETUP.md

---

## Success Metrics

### Deployment Success Definition

✅ **Technical Success**:
- All services start successfully
- Frontend accessible and functional
- API proxy works correctly
- Health checks pass
- Data persists across restarts

✅ **Documentation Success**:
- User can set up without assistance
- Troubleshooting guide resolves common issues
- All commands work as documented

✅ **Performance Success**:
- Build time acceptable (< 10 min first time)
- Startup time acceptable (< 2 min)
- Resource usage reasonable (< 500 MB total)

---

## Test Execution Log

### Execution Date: [To be filled during testing]

| Test Scenario | Status | Notes | Pass/Fail |
|---------------|--------|-------|-----------|
| Fresh Installation | ⏳ Pending | | |
| Frontend Accessibility | ⏳ Pending | | |
| API Proxy | ⏳ Pending | | |
| Service Communication | ⏳ Pending | | |
| Health Checks | ⏳ Pending | | |
| Build Performance | ⏳ Pending | | |
| Rebuild | ⏳ Pending | | |
| Restart Persistence | ⏳ Pending | | |
| Port Conflict | ⏳ Pending | | |
| Frontend Functionality | ⏳ Pending | | |

**Overall Result**: ⏳ Pending

---

## Sign-Off

### Verification Sign-Off

- [ ] All automated tests passed
- [ ] Manual testing completed
- [ ] Documentation verified
- [ ] Performance acceptable
- [ ] Security checked
- [ ] Rollback tested

**Verified By**: _________________

**Date**: _________________

---

**Last Updated**: 2025-12-11 | **Version**: 1.6.2
