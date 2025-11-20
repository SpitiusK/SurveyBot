# RichContentRenderer Quick Start Guide

## 5-Minute Setup

### 1. Installation (Already Done)

The component is ready to use. Dependencies installed:
- ✅ `dompurify` - HTML sanitization
- ✅ `@types/dompurify` - TypeScript types

### 2. Import the Component

```typescript
import { RichContentRenderer } from '@/components/RichContentRenderer';
```

### 3. Basic Usage

```typescript
// Simple text rendering
<RichContentRenderer
  htmlContent="<h3>Question Title</h3><p>Question description</p>"
/>
```

### 4. With Media

```typescript
import type { MediaContentDto } from '@/types/media';

const mediaContent: MediaContentDto = {
  version: '1.0',
  items: [
    {
      id: 'img-1',
      type: 'image',
      filePath: '/uploads/image.jpg',
      thumbnailPath: '/uploads/image-thumb.jpg',
      displayName: 'Product Image',
      fileSize: 512000, // bytes
      mimeType: 'image/jpeg',
      uploadedAt: new Date().toISOString(),
      altText: 'Product photo showing features',
      order: 0,
    },
  ],
};

<RichContentRenderer
  htmlContent="<h3>Review this Product</h3><p>Please provide feedback:</p>"
  mediaContent={mediaContent}
/>
```

## Common Use Cases

### Use Case 1: Display Survey Question

```typescript
// In QuestionDisplay.tsx or similar
function QuestionDisplay({ question }) {
  return (
    <Box sx={{ mb: 3 }}>
      <RichContentRenderer
        htmlContent={question.text}
        mediaContent={question.mediaContent}
      />
    </Box>
  );
}
```

### Use Case 2: Survey Preview

```typescript
// In SurveyPreview.tsx
function SurveyPreview({ survey }) {
  return (
    <>
      {survey.questions.map((question, index) => (
        <Paper key={question.id} sx={{ p: 3, mb: 2 }}>
          <Typography variant="overline" color="text.secondary">
            Question {index + 1}
          </Typography>
          <RichContentRenderer
            htmlContent={question.text}
            mediaContent={question.mediaContent}
          />
        </Paper>
      ))}
    </>
  );
}
```

### Use Case 3: Response Viewing

```typescript
// In ResponseView.tsx
function ResponseView({ response }) {
  return (
    <>
      {response.answers.map((answer) => (
        <Box key={answer.id} sx={{ mb: 4 }}>
          {/* Show question */}
          <RichContentRenderer
            htmlContent={answer.question.text}
            mediaContent={answer.question.mediaContent}
          />

          {/* Show answer */}
          <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Answer:
            </Typography>
            <Typography variant="body1">
              {answer.value}
            </Typography>
          </Box>
        </Box>
      ))}
    </>
  );
}
```

## Key Features

### 1. Automatic XSS Protection
All HTML is automatically sanitized - no action required:

```typescript
// Dangerous input
const html = '<p>Hello</p><script>alert("XSS")</script>';

// Safe output rendered
<RichContentRenderer htmlContent={html} />
// Result: Only "<p>Hello</p>" is rendered
```

### 2. Lazy Loading Images
Images load only when visible - automatic performance optimization:

```typescript
// Images automatically get loading="lazy"
<RichContentRenderer
  htmlContent={htmlContent}
  mediaContent={mediaContent}
/>
```

### 3. Responsive Media
All media scales to fit container:

```typescript
// Works on mobile, tablet, desktop
<RichContentRenderer
  htmlContent="<h3>Video Tutorial</h3>"
  mediaContent={videoMedia}
/>
```

### 4. Accessibility Built-in
ARIA labels, alt text, semantic HTML - all included:

```typescript
// Provide alt text in media items
const media = {
  altText: 'Descriptive text for screen readers',
  // ...other properties
};
```

## Props Quick Reference

| Prop | Type | Required | Default | Example |
|------|------|----------|---------|---------|
| `htmlContent` | `string` | ✅ Yes | - | `"<h3>Title</h3>"` |
| `mediaContent` | `MediaContentDto` | No | `undefined` | `{ version: '1.0', items: [...] }` |
| `readOnly` | `boolean` | No | `true` | `true` |
| `className` | `string` | No | `''` | `"custom-style"` |

## Media Types Supported

### Images
```typescript
{
  type: 'image',
  filePath: '/uploads/photo.jpg',
  thumbnailPath: '/uploads/photo-thumb.jpg',
  altText: 'Description',
  // ... other required fields
}
```

### Videos
```typescript
{
  type: 'video',
  filePath: '/uploads/tutorial.mp4',
  altText: 'Tutorial video',
  // ... other required fields
}
```

### Audio
```typescript
{
  type: 'audio',
  filePath: '/uploads/narration.mp3',
  altText: 'Audio description',
  // ... other required fields
}
```

### Documents
```typescript
{
  type: 'document',
  filePath: '/uploads/guide.pdf',
  displayName: 'User Guide.pdf',
  // ... other required fields
}
```

## Styling Examples

### Custom Text Color
```typescript
<Box sx={{ '& .rich-content-renderer': { color: 'primary.main' } }}>
  <RichContentRenderer htmlContent={content} />
</Box>
```

### Custom Heading Style
```typescript
<Box
  sx={{
    '& .rich-content-renderer h3': {
      color: 'secondary.main',
      borderBottom: '2px solid',
      borderColor: 'secondary.light',
      pb: 1,
    },
  }}
>
  <RichContentRenderer htmlContent={content} />
</Box>
```

### Custom Media Spacing
```typescript
<Box sx={{ '& .media-display': { mt: 4, pt: 4 } }}>
  <RichContentRenderer
    htmlContent={content}
    mediaContent={media}
  />
</Box>
```

## Troubleshooting

### Images not showing?
1. Check `filePath` is correct
2. Verify images are accessible from URL
3. Check browser console for errors

### Content being stripped?
1. Only safe HTML tags are allowed
2. Dangerous content is automatically removed
3. This is a security feature, not a bug

### Styling not working?
1. Use `sx` prop on parent Box
2. Target `.rich-content-renderer` class
3. Check CSS specificity

## Next Steps

1. **Full Documentation**: See `RichContentRenderer.README.md`
2. **Examples**: See `RichContentRenderer.example.tsx`
3. **Integration**: Update QuestionDisplay component
4. **Testing**: Create tests for your use case

## Files Created

- `RichContentRenderer.tsx` - Main component
- `RichContentRenderer.css` - Styles
- `RichContentRenderer.README.md` - Full documentation
- `RichContentRenderer.example.tsx` - Usage examples
- `RichContentRenderer.QUICKSTART.md` - This file

## Quick Integration Checklist

- [ ] Import component in target file
- [ ] Pass `htmlContent` from question
- [ ] Pass `mediaContent` if available
- [ ] Test with different question types
- [ ] Verify security (try XSS payload)
- [ ] Test responsive behavior
- [ ] Check accessibility (screen reader)
- [ ] Verify print preview

## Support

For detailed information, see the full README or refer to task documentation at:
`C:\Users\User\Desktop\SurveyBot\multimedia-task-flow.md`

**Component Location**: `C:\Users\User\Desktop\SurveyBot\frontend\src\components\RichContentRenderer.tsx`

**Last Updated**: 2025-11-19
