# Manual Testing Checklist - Bot Media Handling

**Purpose**: Comprehensive manual testing checklist for Telegram bot media features
**Date**: 2025-11-19
**Version**: 1.0.0

---

## Prerequisites

- [ ] Telegram bot is running (polling or webhook mode)
- [ ] API is running and accessible
- [ ] Database has test surveys with media attached
- [ ] Test Telegram account ready
- [ ] Bot username: @YourBotName (replace with actual)

---

## Test Suite 1: Image Display

### Basic Image Display

- [ ] **Test**: Create survey with single image question via admin panel
- [ ] **Expected**: Question shows 1 image in admin panel
- [ ] **Test**: Take survey via Telegram bot
- [ ] **Expected**: Image displays BEFORE question text
- [ ] **Expected**: Image loads correctly (not broken)
- [ ] **Expected**: Caption shows "Media 1 of 1"
- [ ] **Expected**: Question text appears after image
- [ ] **Test**: Click/tap image in Telegram
- [ ] **Expected**: Image opens in full size (if supported by Telegram)

### Multiple Images

- [ ] **Test**: Create question with 3 images
- [ ] **Expected**: All 3 images sent in correct order
- [ ] **Expected**: Captions show "Media 1 of 3", "Media 2 of 3", "Media 3 of 3"
- [ ] **Expected**: Question text appears after last image

### Image Formats

- [ ] **Test**: Upload JPG image (max 10MB)
- [ ] **Expected**: Image displays correctly
- [ ] **Test**: Upload PNG image with transparency
- [ ] **Expected**: Image displays correctly
- [ ] **Test**: Upload GIF image
- [ ] **Expected**: GIF displays and animates (if animated)
- [ ] **Test**: Upload WebP image
- [ ] **Expected**: Image displays correctly

### Image Size Limits

- [ ] **Test**: Upload 100KB image
- [ ] **Expected**: Loads instantly, displays correctly
- [ ] **Test**: Upload 5MB image
- [ ] **Expected**: Loads within 2-3 seconds, displays correctly
- [ ] **Test**: Upload 10MB image (max size)
- [ ] **Expected**: Loads within 5 seconds, displays correctly
- [ ] **Test**: Attempt 15MB image (over limit)
- [ ] **Expected**: Admin panel rejects with error message

---

## Test Suite 2: Video Display

### Basic Video Display

- [ ] **Test**: Create question with video file
- [ ] **Expected**: Video displays with Telegram video player
- [ ] **Expected**: Video thumbnail shows before playing
- [ ] **Expected**: Caption shows "Media 1 of 1"
- [ ] **Test**: Click play button
- [ ] **Expected**: Video plays smoothly
- [ ] **Test**: Pause video
- [ ] **Expected**: Video pauses correctly
- [ ] **Test**: Adjust volume
- [ ] **Expected**: Volume controls work

### Video Formats

- [ ] **Test**: Upload MP4 video (H.264 codec)
- [ ] **Expected**: Video plays correctly
- [ ] **Test**: Upload MOV video
- [ ] **Expected**: Video plays correctly
- [ ] **Test**: Upload AVI video
- [ ] **Expected**: Video plays correctly (may be converted)

### Video Size Limits

- [ ] **Test**: Upload 10MB video
- [ ] **Expected**: Loads and plays correctly
- [ ] **Test**: Upload 50MB video (max size)
- [ ] **Expected**: Loads within 10 seconds, plays correctly
- [ ] **Test**: Attempt 60MB video (over limit)
- [ ] **Expected**: Admin panel rejects with error message

---

## Test Suite 3: Audio Display

### Basic Audio Display

- [ ] **Test**: Create question with audio file
- [ ] **Expected**: Audio displays with Telegram audio player
- [ ] **Expected**: Shows audio duration
- [ ] **Expected**: Caption shows "Media 1 of 1"
- [ ] **Test**: Click play button
- [ ] **Expected**: Audio plays correctly
- [ ] **Test**: Seek to middle of audio
- [ ] **Expected**: Seeking works correctly

### Audio Formats

- [ ] **Test**: Upload MP3 audio
- [ ] **Expected**: Audio plays correctly
- [ ] **Test**: Upload WAV audio
- [ ] **Expected**: Audio plays correctly
- [ ] **Test**: Upload OGG audio
- [ ] **Expected**: Audio plays correctly

### Audio Size Limits

- [ ] **Test**: Upload 5MB audio
- [ ] **Expected**: Loads and plays correctly
- [ ] **Test**: Upload 20MB audio (max size)
- [ ] **Expected**: Loads within 5 seconds, plays correctly
- [ ] **Test**: Attempt 25MB audio (over limit)
- [ ] **Expected**: Admin panel rejects with error message

---

## Test Suite 4: Document Display

### Basic Document Display

- [ ] **Test**: Create question with PDF document
- [ ] **Expected**: Document shows with download icon
- [ ] **Expected**: File name displayed correctly
- [ ] **Expected**: File size displayed correctly
- [ ] **Expected**: Caption shows "Media 1 of 1"
- [ ] **Test**: Click/tap document
- [ ] **Expected**: Document downloads or opens in viewer

### Document Types

- [ ] **Test**: Upload PDF document
- [ ] **Expected**: PDF icon shows, can be opened
- [ ] **Test**: Upload DOCX document
- [ ] **Expected**: Document downloads correctly
- [ ] **Test**: Upload TXT document
- [ ] **Expected**: Text file can be viewed
- [ ] **Test**: Upload ZIP file
- [ ] **Expected**: ZIP file can be downloaded

### Document Size Limits

- [ ] **Test**: Upload 1MB PDF
- [ ] **Expected**: Downloads/opens correctly
- [ ] **Test**: Upload 10MB PDF
- [ ] **Expected**: Downloads within 3 seconds
- [ ] **Test**: Upload 20MB document (max size)
- [ ] **Expected**: Downloads within 10 seconds
- [ ] **Test**: Attempt 25MB document (over limit)
- [ ] **Expected**: Admin panel rejects with error message

---

## Test Suite 5: Multiple Media Types

### Mixed Media in One Question

- [ ] **Test**: Create question with 2 images + 1 video
- [ ] **Expected**: All 3 media items sent in correct order
- [ ] **Expected**: Images display first, then video
- [ ] **Expected**: Captions show "Media 1 of 3", "Media 2 of 3", "Media 3 of 3"
- [ ] **Test**: Create question with image + audio + document
- [ ] **Expected**: All 3 media items display correctly in order
- [ ] **Test**: Create question with 5 mixed media items
- [ ] **Expected**: All 5 items sent with correct captions

### Order Preservation

- [ ] **Test**: Create question with media in specific order: video, image, audio
- [ ] **Expected**: Media displays in exact order specified
- [ ] **Test**: Reorder media in admin panel (move last item to first)
- [ ] **Expected**: New order reflected when taking survey

---

## Test Suite 6: Error Scenarios

### Invalid Media URLs

- [ ] **Test**: Manually create question with invalid image URL (database edit)
- [ ] **Expected**: Bot skips broken image, continues to question text
- [ ] **Expected**: User can still answer question
- [ ] **Expected**: Log shows warning about failed media
- [ ] **Test**: Delete uploaded file from filesystem, keep DB reference
- [ ] **Expected**: Bot handles gracefully, shows question text

### Network Issues

- [ ] **Test**: Disable internet briefly while bot sends media
- [ ] **Expected**: Bot retries (up to 3 times)
- [ ] **Expected**: If all retries fail, shows question text anyway
- [ ] **Expected**: Logs show retry attempts
- [ ] **Test**: Slow network (throttle to 100KB/s)
- [ ] **Expected**: Large media takes longer but eventually loads
- [ ] **Expected**: No timeout errors for media under size limits

### Telegram Rate Limiting

- [ ] **Test**: Take survey with 10 media items rapidly
- [ ] **Expected**: Bot sends media with small delays between each
- [ ] **Expected**: All media eventually sent
- [ ] **Expected**: No "Too Many Requests" errors

### Malformed Media Content JSON

- [ ] **Test**: Manually edit question MediaContent to invalid JSON (database edit)
- [ ] **Expected**: Bot handles gracefully, skips media
- [ ] **Expected**: Question text still displays
- [ ] **Expected**: User can answer question
- [ ] **Test**: Set MediaContent to empty string
- [ ] **Expected**: Treated as no media, question displays normally

---

## Test Suite 7: Preview Command

### Basic Preview

- [ ] **Test**: Create survey with 3 questions (1 with image, 1 text-only, 1 with video)
- [ ] **Test**: Run command: `/preview <survey_id>`
- [ ] **Expected**: Preview shows all 3 questions
- [ ] **Expected**: Question 1 shows "Media: 1 image 📷"
- [ ] **Expected**: Question 2 shows "Media: None"
- [ ] **Expected**: Question 3 shows "Media: 1 video 🎬"

### Multiple Media Preview

- [ ] **Test**: Create question with 3 images + 2 videos
- [ ] **Test**: Run `/preview <survey_id>`
- [ ] **Expected**: Preview shows "Media: 3 images 📷, 2 videos 🎬"

### Preview Accuracy

- [ ] **Test**: Create survey with mixed media types
- [ ] **Expected**: Preview counts are accurate
- [ ] **Expected**: Media type emojis correct (📷 🎬 🎵 📄)
- [ ] **Test**: Add media to question, run preview again
- [ ] **Expected**: Updated media count reflected

---

## Test Suite 8: Survey Flow with Media

### Complete Survey Flow

- [ ] **Test**: Create full survey: 5 questions, 3 with media
- [ ] **Test**: Start survey with `/survey <code>`
- [ ] **Expected**: Survey starts successfully
- [ ] **Test**: Question 1 (with media) displays
- [ ] **Expected**: Media loads before question text
- [ ] **Test**: Answer Question 1
- [ ] **Expected**: Advances to Question 2
- [ ] **Test**: Complete all 5 questions
- [ ] **Expected**: Survey completes successfully
- [ ] **Expected**: Completion message shows

### Navigation with Media

- [ ] **Test**: Start survey with media questions
- [ ] **Test**: On Question 2, click "⬅️ Back" button
- [ ] **Expected**: Returns to Question 1
- [ ] **Expected**: Media displays again (not cached)
- [ ] **Test**: Navigate forward again
- [ ] **Expected**: Media displays on Question 2

### Skip with Media

- [ ] **Test**: Create optional question with media
- [ ] **Test**: Take survey, reach optional media question
- [ ] **Test**: Click "Skip ⏭️" button
- [ ] **Expected**: Skips to next question without error
- [ ] **Expected**: Media was displayed before skip option

---

## Test Suite 9: Performance

### Load Time

- [ ] **Test**: Question with single 1MB image
- [ ] **Measure**: Time from bot message to image displayed
- [ ] **Expected**: < 2 seconds on good connection
- [ ] **Test**: Question with 5MB video
- [ ] **Measure**: Time to video player appears
- [ ] **Expected**: < 5 seconds on good connection

### Multiple Media Load Time

- [ ] **Test**: Question with 5 images (each 2MB)
- [ ] **Measure**: Time for all 5 images to load
- [ ] **Expected**: < 10 seconds total
- [ ] **Expected**: Small delay (100ms) between each image

### Survey Completion Time

- [ ] **Test**: 10-question survey with media on every question
- [ ] **Measure**: Total time from start to completion
- [ ] **Expected**: Media loading doesn't significantly slow survey
- [ ] **Expected**: User can answer while media loads

---

## Test Suite 10: Cross-Platform

### Telegram Desktop

- [ ] **Test**: Take media survey on Telegram Desktop (Windows/Mac/Linux)
- [ ] **Expected**: All media types display correctly
- [ ] **Expected**: Video player controls work
- [ ] **Expected**: Images can be clicked to full screen

### Telegram Mobile

- [ ] **Test**: Take media survey on Telegram Mobile (iOS/Android)
- [ ] **Expected**: All media types display correctly
- [ ] **Expected**: Touch controls work for video/audio
- [ ] **Expected**: Images can be tapped to full screen

### Telegram Web

- [ ] **Test**: Take media survey on Telegram Web
- [ ] **Expected**: All media types display correctly
- [ ] **Expected**: Media players work in browser

---

## Test Suite 11: Edge Cases

### Empty Survey

- [ ] **Test**: Create survey with no media
- [ ] **Test**: Take survey via bot
- [ ] **Expected**: Works exactly as before (no regression)

### All Media Questions

- [ ] **Test**: Create survey where EVERY question has media
- [ ] **Expected**: All media loads correctly
- [ ] **Expected**: Survey completes successfully

### Unicode Filenames

- [ ] **Test**: Upload file with unicode name (e.g., "照片.jpg", "фото.png")
- [ ] **Expected**: File uploads successfully
- [ ] **Expected**: Displays correctly in Telegram

### Special Characters in Filenames

- [ ] **Test**: Upload file with spaces in name ("my photo.jpg")
- [ ] **Expected**: File uploads and displays correctly
- [ ] **Test**: Upload file with special chars ("file-v2.0_final(1).pdf")
- [ ] **Expected**: File uploads and displays correctly

---

## Test Suite 12: Admin Panel Integration

### Upload Media

- [ ] **Test**: Click "Add Media" button in admin panel
- [ ] **Expected**: File picker opens
- [ ] **Test**: Select image file
- [ ] **Expected**: File uploads with progress bar
- [ ] **Expected**: Thumbnail appears in media list

### Remove Media

- [ ] **Test**: Click "Remove" button on media item
- [ ] **Expected**: Confirmation dialog appears
- [ ] **Test**: Confirm removal
- [ ] **Expected**: Media removed from list
- [ ] **Expected**: File remains in filesystem (soft delete)

### Reorder Media

- [ ] **Test**: Drag media item to different position
- [ ] **Expected**: Order updates immediately
- [ ] **Test**: Save question
- [ ] **Expected**: New order persisted to database

---

## Testing Notes

### Environment

- **Bot Mode**: [  ] Polling  [  ] Webhook
- **API URL**: _______________________
- **Database**: PostgreSQL version ____
- **Telegram Client**: [  ] Desktop  [  ] Mobile  [  ] Web

### Issues Found

| Test ID | Issue Description | Severity | Status |
|---------|-------------------|----------|--------|
| | | | |
| | | | |

### Performance Metrics

| Media Type | File Size | Load Time | Notes |
|------------|-----------|-----------|-------|
| Image | ___ MB | ___ sec | |
| Video | ___ MB | ___ sec | |
| Audio | ___ MB | ___ sec | |
| Document | ___ MB | ___ sec | |

---

## Sign-Off

- **Tester Name**: _______________________
- **Date**: _______________________
- **Test Environment**: [  ] Development  [  ] Staging  [  ] Production
- **Overall Result**: [  ] Pass  [  ] Fail  [  ] Pass with Issues

---

## Appendix: Test Data

### Sample Test Surveys

1. **Basic Media Survey** (ID: _____)
   - Question 1: Text + 1 image
   - Question 2: Text only
   - Question 3: Single choice + 1 video

2. **Heavy Media Survey** (ID: _____)
   - Question 1: Text + 5 images
   - Question 2: Multiple choice + 2 videos
   - Question 3: Rating + 1 audio

3. **Mixed Media Survey** (ID: _____)
   - Question 1: Text + image + video + audio + document

### Sample Media Files

- `test-image-small.jpg` (500KB)
- `test-image-large.jpg` (9MB)
- `test-video-short.mp4` (5MB, 30 seconds)
- `test-video-long.mp4` (45MB, 5 minutes)
- `test-audio.mp3` (3MB, 3 minutes)
- `test-document.pdf` (2MB, 10 pages)

---

**End of Manual Testing Checklist**
