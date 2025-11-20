# RichContentRenderer Component

## Overview

The `RichContentRenderer` component is a secure, accessible, and performant component for displaying rich HTML content with embedded media in the SurveyBot application. It's used primarily for rendering question text and descriptions in survey previews and when users are taking surveys.

## Features

### Security
- **XSS Protection**: Uses DOMPurify to sanitize HTML and prevent cross-site scripting attacks
- **Tag Whitelisting**: Only allows safe HTML tags (headings, paragraphs, lists, etc.)
- **Attribute Filtering**: Strips dangerous attributes while preserving safe ones like `href`, `src`, `alt`
- **Content Preservation**: Maintains safe content while removing malicious code

### Performance
- **Lazy Loading**: Images load only when visible using Intersection Observer API
- **Efficient Re-rendering**: Only updates when content changes
- **Optimized CSS**: Uses CSS-based styling without runtime style generation

### Accessibility
- **Semantic HTML**: Uses proper HTML5 elements (figure, figcaption, article)
- **Alt Text**: Images display alt text from media metadata
- **ARIA Labels**: Media controls have proper ARIA labels
- **Keyboard Navigation**: Full keyboard support for media controls
- **Screen Reader Friendly**: Proper role attributes and descriptions

### Responsive Design
- **Mobile-First**: Optimized for all screen sizes
- **Responsive Media**: Images and videos scale appropriately
- **Touch-Friendly**: Controls work well on touch devices
- **Print Support**: Optimized styles for printing

### Media Types
- **Images**: Responsive images with lazy loading and alt text
- **Videos**: HTML5 video player with controls
- **Audio**: HTML5 audio player with controls
- **Documents**: Download links with file size display

## Installation

### Prerequisites

The component requires DOMPurify to be installed:

```bash
npm install dompurify
npm install --save-dev @types/dompurify
```

### Import

```typescript
import { RichContentRenderer } from '@/components/RichContentRenderer';
import type { MediaContentDto } from '@/types/media';
```

## API Reference

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `htmlContent` | `string` | Required | HTML content to render (will be sanitized) |
| `mediaContent` | `MediaContentDto \| undefined` | `undefined` | Media items to display |
| `readOnly` | `boolean` | `true` | Whether content is read-only (affects ARIA roles) |
| `className` | `string` | `''` | Additional CSS class name |

### MediaContentDto Structure

```typescript
interface MediaContentDto {
  version: string;
  items: MediaItemDto[];
}

interface MediaItemDto {
  id: string;
  type: 'image' | 'video' | 'audio' | 'document';
  filePath: string;
  displayName: string;
  fileSize: number;
  mimeType: string;
  uploadedAt: string;
  altText?: string;
  thumbnailPath?: string;
  order: number;
}
```

## Usage Examples

### Basic Usage

```typescript
import { RichContentRenderer } from '@/components/RichContentRenderer';

function QuestionDisplay({ question }) {
  return (
    <RichContentRenderer
      htmlContent={question.text}
      mediaContent={question.mediaContent}
    />
  );
}
```

### With Custom Styling

```typescript
<RichContentRenderer
  htmlContent={htmlContent}
  mediaContent={mediaContent}
  className="custom-question-style"
/>
```

### Survey Question Example

```typescript
function SurveyQuestion({ question }) {
  const htmlContent = `
    <h3>${question.title}</h3>
    <p>${question.description}</p>
  `;

  return (
    <Box sx={{ mb: 3 }}>
      <RichContentRenderer
        htmlContent={htmlContent}
        mediaContent={question.mediaContent}
      />
      {/* Answer input components */}
    </Box>
  );
}
```

### Multiple Media Items

```typescript
const mediaContent: MediaContentDto = {
  version: '1.0',
  items: [
    {
      id: '1',
      type: 'image',
      filePath: '/uploads/image.jpg',
      thumbnailPath: '/uploads/image-thumb.jpg',
      displayName: 'Diagram',
      fileSize: 1024 * 500,
      mimeType: 'image/jpeg',
      uploadedAt: new Date().toISOString(),
      altText: 'Process flow diagram',
      order: 0,
    },
    {
      id: '2',
      type: 'video',
      filePath: '/uploads/tutorial.mp4',
      displayName: 'Tutorial Video',
      fileSize: 1024 * 1024 * 10,
      mimeType: 'video/mp4',
      uploadedAt: new Date().toISOString(),
      altText: 'Step-by-step tutorial',
      order: 1,
    },
  ],
};

<RichContentRenderer
  htmlContent="<h3>Review Materials</h3><p>Please review the following:</p>"
  mediaContent={mediaContent}
/>
```

## HTML Sanitization

### Allowed Tags

The component allows the following HTML tags:

- **Text**: `p`, `div`, `span`, `br`, `hr`
- **Formatting**: `strong`, `em`, `u`, `s`
- **Headings**: `h1`, `h2`, `h3`, `h4`, `h5`, `h6`
- **Lists**: `ul`, `ol`, `li`
- **Other**: `blockquote`, `code`, `pre`, `a`, `img`

### Allowed Attributes

Only safe attributes are preserved:
- `href` (for links)
- `src` (for images)
- `alt` (for images)
- `title`
- `class`
- `style` (inline styles)

### Removed Content

The following are automatically stripped:
- `<script>` tags and JavaScript code
- Event handlers (`onclick`, `onerror`, etc.)
- `<iframe>` and `<object>` tags
- `javascript:` URLs
- Any other potentially dangerous content

### Example: Before and After Sanitization

**Before (dangerous input):**
```html
<p>Hello <strong>User</strong></p>
<script>alert('XSS!');</script>
<p onclick="steal()">Click me</p>
<img src="x" onerror="malicious()">
```

**After (safe output):**
```html
<p>Hello <strong>User</strong></p>

<p>Click me</p>
<img src="x">
```

## Styling

### CSS Classes

The component provides several CSS classes for customization:

```css
.rich-content-renderer       /* Root container */
.content-text                /* HTML content wrapper */
.media-display               /* Media gallery container */
.media-item                  /* Individual media item */
.media-figure                /* Figure element for media */
.media-image                 /* Image element */
.media-video                 /* Video element */
.media-audio                 /* Audio element */
.media-caption               /* Media caption */
.media-document              /* Document container */
.document-link               /* Download link */
```

### Custom Styling Example

```typescript
// In your component
<Box
  sx={{
    '& .rich-content-renderer': {
      fontSize: '18px',
      '& h2': {
        color: 'primary.main',
      },
    },
  }}
>
  <RichContentRenderer htmlContent={content} />
</Box>
```

### Dark Mode

The component includes built-in dark mode support using `prefers-color-scheme`:

```css
@media (prefers-color-scheme: dark) {
  .rich-content-renderer {
    color: #e0e0e0;
  }
  /* Additional dark mode styles... */
}
```

### Print Styles

Optimized for printing with clean, readable output:

```css
@media print {
  .media-image,
  .media-video {
    max-width: 100%;
    page-break-inside: avoid;
  }
  /* Hides media players in print */
  .media-audio,
  .media-video {
    display: none;
  }
}
```

## Performance Optimization

### Lazy Loading

Images use the Intersection Observer API for lazy loading:

```typescript
// Automatically applied to all images
<img src="image.jpg" loading="lazy" />
```

If you need images to load immediately:

```html
<!-- In your HTML content -->
<img src="critical.jpg" loading="eager" />
```

### Re-rendering Optimization

The component only re-renders when `htmlContent` changes:

```typescript
useEffect(() => {
  if (contentRef.current && htmlContent) {
    const cleanHtml = DOMPurify.sanitize(htmlContent, config);
    contentRef.current.innerHTML = cleanHtml;
    setupLazyLoading(contentRef.current);
  }
}, [htmlContent]);
```

## Accessibility

### ARIA Roles

```typescript
<div
  ref={contentRef}
  className="content-text"
  role={readOnly ? 'article' : undefined}
/>
```

### Alt Text

Always provide alt text for images:

```typescript
const media: MediaItemDto = {
  // ... other properties
  altText: 'Descriptive text for screen readers',
};
```

### Media Controls

All media elements include proper ARIA labels:

```html
<video
  controls
  title="Video title"
  aria-label="Descriptive label"
>
```

### Keyboard Navigation

All interactive elements (links, media controls) are keyboard-accessible using standard HTML elements.

## Integration with Survey Components

### In QuestionDisplay Component

```typescript
// src/components/QuestionDisplay.tsx
import { RichContentRenderer } from './RichContentRenderer';

interface QuestionDisplayProps {
  question: Question;
}

export const QuestionDisplay: React.FC<QuestionDisplayProps> = ({ question }) => {
  return (
    <Box sx={{ mb: 3 }}>
      <RichContentRenderer
        htmlContent={question.text}
        mediaContent={question.mediaContent}
      />

      {/* Render answer input based on question type */}
      {renderAnswerInput(question)}
    </Box>
  );
};
```

### In Survey Preview

```typescript
// src/pages/SurveyPreview.tsx
import { RichContentRenderer } from '@/components/RichContentRenderer';

export const SurveyPreview: React.FC = () => {
  return (
    <Box>
      {survey.questions.map((question) => (
        <Paper key={question.id} sx={{ p: 3, mb: 2 }}>
          <RichContentRenderer
            htmlContent={question.text}
            mediaContent={question.mediaContent}
          />
        </Paper>
      ))}
    </Box>
  );
};
```

### In Response View

```typescript
// src/pages/ResponseView.tsx
export const ResponseView: React.FC = () => {
  return (
    <Box>
      {response.answers.map((answer) => (
        <Box key={answer.id} sx={{ mb: 3 }}>
          <RichContentRenderer
            htmlContent={answer.question.text}
            mediaContent={answer.question.mediaContent}
          />
          <Typography variant="body1" sx={{ mt: 2, p: 2, bgcolor: 'grey.50' }}>
            Answer: {answer.value}
          </Typography>
        </Box>
      ))}
    </Box>
  );
};
```

## Browser Support

- Modern browsers with Intersection Observer support (lazy loading)
- Falls back gracefully in older browsers (images load immediately)
- Tested on:
  - Chrome 90+
  - Firefox 88+
  - Safari 14+
  - Edge 90+

## Troubleshooting

### Images Not Loading

**Problem**: Images appear as broken links

**Solutions**:
1. Verify `filePath` and `thumbnailPath` are correct
2. Check file permissions and accessibility
3. Ensure images are served from correct domain
4. Check browser console for CORS errors

### Media Not Playing

**Problem**: Videos or audio won't play

**Solutions**:
1. Verify file format is supported (MP4, WebM, MP3, OGG)
2. Check MIME type is correct
3. Ensure file is accessible from the URL
4. Try different browser

### Styling Not Applied

**Problem**: Custom styles not working

**Solutions**:
1. Check CSS specificity
2. Ensure className is passed correctly
3. Verify CSS file is imported
4. Use `!important` if needed (last resort)

### Content Sanitization Too Aggressive

**Problem**: Needed content is being removed

**Solutions**:
1. Check if the HTML tag is in `ALLOWED_TAGS`
2. Verify attributes are in `ALLOWED_ATTR`
3. Modify DOMPurify config if necessary (with caution)

### Performance Issues

**Problem**: Slow rendering with many images

**Solutions**:
1. Ensure lazy loading is working (check Intersection Observer)
2. Optimize image sizes before upload
3. Use thumbnails for preview
4. Limit number of media items per question

## Security Best Practices

1. **Never disable sanitization**: Always use DOMPurify
2. **Validate on backend**: Frontend sanitization is not enough
3. **Use HTTPS**: Serve all media over HTTPS
4. **Set CSP headers**: Configure Content Security Policy
5. **Regular updates**: Keep DOMPurify updated

## Testing

### Unit Tests Example

```typescript
import { render, screen } from '@testing-library/react';
import { RichContentRenderer } from './RichContentRenderer';

describe('RichContentRenderer', () => {
  it('renders HTML content safely', () => {
    const html = '<p>Test <strong>content</strong></p>';
    render(<RichContentRenderer htmlContent={html} />);
    expect(screen.getByText(/Test content/i)).toBeInTheDocument();
  });

  it('sanitizes dangerous content', () => {
    const html = '<p>Safe</p><script>alert("XSS")</script>';
    render(<RichContentRenderer htmlContent={html} />);
    expect(screen.queryByText(/alert/i)).not.toBeInTheDocument();
  });

  it('renders media items', () => {
    const mediaContent = {
      version: '1.0',
      items: [{
        id: '1',
        type: 'image' as const,
        filePath: '/test.jpg',
        displayName: 'Test',
        fileSize: 1024,
        mimeType: 'image/jpeg',
        uploadedAt: new Date().toISOString(),
        altText: 'Test image',
        order: 0,
      }],
    };

    render(
      <RichContentRenderer
        htmlContent="<p>Test</p>"
        mediaContent={mediaContent}
      />
    );

    expect(screen.getByAltText('Test image')).toBeInTheDocument();
  });
});
```

## Related Components

- **RichTextEditor**: For editing rich content
- **MediaPicker**: For uploading media
- **MediaGallery**: For managing media items
- **QuestionDisplay**: Uses RichContentRenderer for displaying questions

## Changelog

### Version 1.0.0 (2025-11-19)
- Initial release
- HTML sanitization with DOMPurify
- Media rendering (image, video, audio, document)
- Lazy loading support
- Accessibility features
- Responsive design
- Dark mode support
- Print optimization

## Future Enhancements

- [ ] Inline media positioning (not just gallery)
- [ ] Image zoom/lightbox functionality
- [ ] Caption editing for media
- [ ] Emoji support
- [ ] Table support
- [ ] Code syntax highlighting
- [ ] Math equation rendering (MathJax)
- [ ] PDF preview inline
- [ ] Audio waveform visualization

## License

Part of the SurveyBot project.

## Support

For issues or questions, refer to the main project documentation at `C:\Users\User\Desktop\SurveyBot\CLAUDE.md`.
