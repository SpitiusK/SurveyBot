# RichTextEditor Component

A comprehensive rich text editor with integrated media upload support, built with ReactQuill and Material-UI.

## Overview

The `RichTextEditor` component combines text formatting capabilities with drag-and-drop media uploads. It's designed for creating rich survey questions with optional multimedia attachments.

## Features

- Rich text formatting (bold, italic, underline, strike, etc.)
- Lists (ordered, unordered)
- Headers (H1, H2)
- Links and blockquotes
- Code blocks
- Integrated media upload modal
- Media gallery display with previews
- Delete media functionality
- Read-only mode for viewing
- Responsive design
- Accessibility support

## Installation

The component requires these dependencies (already installed):

```bash
npm install react-quill @mui/material @mui/icons-material react-dropzone
```

## Basic Usage

```typescript
import { RichTextEditor } from '@/components/RichTextEditor';
import { MediaContentDto } from '@/types/media';

function MyComponent() {
  const [content, setContent] = useState('');
  const [media, setMedia] = useState<MediaContentDto>();

  const handleChange = (text: string, mediaContent?: MediaContentDto) => {
    setContent(text);
    setMedia(mediaContent);
  };

  return (
    <RichTextEditor
      value={content}
      onChange={handleChange}
      placeholder="Enter your text here..."
      mediaType="image"
    />
  );
}
```

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `string` | Required | Current text content |
| `onChange` | `(content: string, mediaContent?: MediaContentDto) => void` | Required | Callback when content changes |
| `onError` | `(error: string) => void` | Optional | Error handler for media uploads |
| `placeholder` | `string` | `'Enter question text...'` | Placeholder text |
| `readOnly` | `boolean` | `false` | Read-only mode (hides toolbar) |
| `mediaType` | `'image' \| 'video' \| 'audio' \| 'document'` | `'image'` | Type of media to upload |
| `initialMedia` | `MediaItemDto[]` | `[]` | Pre-loaded media items |

## Advanced Usage

### With Form Integration (React Hook Form)

```typescript
import { Controller, useForm } from 'react-hook-form';
import { RichTextEditor } from '@/components/RichTextEditor';

interface FormData {
  questionText: string;
  mediaContent?: MediaContentDto;
}

function QuestionForm() {
  const { control, setValue } = useForm<FormData>();

  return (
    <Controller
      name="questionText"
      control={control}
      render={({ field }) => (
        <RichTextEditor
          value={field.value}
          onChange={(text, media) => {
            field.onChange(text);
            setValue('mediaContent', media);
          }}
          placeholder="Enter your question..."
          mediaType="image"
        />
      )}
    />
  );
}
```

### With Initial Media Items

```typescript
const existingMedia: MediaItemDto[] = [
  {
    id: '123',
    type: 'image',
    filePath: '/uploads/image.jpg',
    thumbnailPath: '/uploads/image_thumb.jpg',
    displayName: 'Photo 1.jpg',
    fileSize: 2048000,
    mimeType: 'image/jpeg',
    uploadedAt: '2025-11-19T00:00:00Z',
    order: 0,
  },
];

<RichTextEditor
  value={content}
  onChange={handleChange}
  initialMedia={existingMedia}
  mediaType="image"
/>
```

### Read-Only Mode

```typescript
<RichTextEditor
  value={questionText}
  onChange={() => {}} // No-op
  readOnly={true}
  initialMedia={questionMedia}
/>
```

### Multiple Media Types

```typescript
// For video questions
<RichTextEditor
  value={content}
  onChange={handleChange}
  mediaType="video"
  placeholder="Describe the video prompt..."
/>

// For audio questions
<RichTextEditor
  value={content}
  onChange={handleChange}
  mediaType="audio"
  placeholder="Describe the audio question..."
/>

// For documents
<RichTextEditor
  value={content}
  onChange={handleChange}
  mediaType="document"
  placeholder="Upload reference documents..."
/>
```

### Error Handling

```typescript
const [errorMessage, setErrorMessage] = useState<string | null>(null);

<RichTextEditor
  value={content}
  onChange={handleChange}
  onError={(error) => {
    setErrorMessage(error);
    console.error('Media upload error:', error);
  }}
/>

{errorMessage && (
  <Alert severity="error" onClose={() => setErrorMessage(null)}>
    {errorMessage}
  </Alert>
)}
```

## Component Structure

### Toolbar Buttons

The editor includes these formatting options:

1. **Text Formatting**: Bold, Italic, Underline, Strike
2. **Block Elements**: Blockquote, Code Block
3. **Headers**: H1, H2
4. **Lists**: Ordered, Unordered
5. **Links**: Insert/edit hyperlinks
6. **Media**: Custom button to open upload modal
7. **Clear Formatting**: Remove all formatting

### Media Gallery

When media items are attached, they appear below the editor in a responsive grid:

- **Image**: Shows thumbnail preview
- **Video/Audio/Document**: Shows type-specific icon
- **Delete Button**: Appears on hover (hidden in read-only mode)
- **File Info**: Display name and file size

### Media Upload Modal

Clicking the media button opens a dialog containing:

- **MediaPicker Component**: Drag-and-drop upload interface
- **Progress Indicator**: Shows upload progress
- **Error Messages**: Displays validation or upload errors
- **Close/Cancel**: Dismiss the modal

## Styling

### Import Required Styles

```typescript
import 'react-quill/dist/quill.snow.css';
import './RichTextEditor.css'; // Optional custom overrides
```

### Custom Styling

The component uses Material-UI's `sx` prop for styling. You can override styles:

```typescript
<Box sx={{ '& .ql-editor': { minHeight: '500px' } }}>
  <RichTextEditor {...props} />
</Box>
```

### CSS Classes

Available CSS classes for custom styling:

- `.rich-text-editor-container`: Main wrapper
- `.ql-toolbar`: Quill toolbar
- `.ql-container`: Editor container
- `.ql-editor`: Editable content area
- `.media-gallery-section`: Media gallery wrapper
- `.media-card`: Individual media card
- `.delete-button`: Media delete button

## Data Format

### MediaContentDto

```typescript
interface MediaContentDto {
  version: string;        // Format version (e.g., '1.0')
  items: MediaItemDto[];  // Array of media items
}
```

### MediaItemDto

```typescript
interface MediaItemDto {
  id: string;
  type: 'image' | 'video' | 'audio' | 'document';
  filePath: string;
  thumbnailPath?: string;
  displayName: string;
  fileSize: number;       // in bytes
  mimeType: string;
  uploadedAt: string;     // ISO date string
  altText?: string;
  order: number;          // Display order
}
```

## Accessibility

The component includes several accessibility features:

- **Keyboard Navigation**: All toolbar buttons are keyboard accessible
- **ARIA Labels**: Proper labels for screen readers
- **Focus Indicators**: Visible focus outlines
- **Alt Text Support**: For uploaded images
- **Semantic HTML**: Proper heading structure

### Keyboard Shortcuts

Standard Quill keyboard shortcuts are supported:

- `Ctrl/Cmd + B`: Bold
- `Ctrl/Cmd + I`: Italic
- `Ctrl/Cmd + U`: Underline
- `Ctrl/Cmd + K`: Insert link

## Performance Considerations

### Debouncing

For expensive operations (e.g., auto-save), debounce the `onChange` callback:

```typescript
import { debounce } from 'lodash';

const debouncedSave = useMemo(
  () => debounce((text: string, media?: MediaContentDto) => {
    saveToBackend(text, media);
  }, 1000),
  []
);

<RichTextEditor
  value={content}
  onChange={debouncedSave}
/>
```

### Large Media Files

The component doesn't handle extremely large files well. Set appropriate size limits in `MediaPicker`:

```typescript
// Already configured in MediaFileSizeLimits
{
  image: 10 MB,
  video: 50 MB,
  audio: 20 MB,
  document: 25 MB,
}
```

## Integration Example (QuestionForm)

```typescript
import { RichTextEditor } from '@/components/RichTextEditor';
import { MediaContentDto } from '@/types/media';

interface QuestionFormData {
  type: 'Text' | 'SingleChoice' | 'MultipleChoice' | 'Rating';
  questionText: string;
  mediaContent?: MediaContentDto;
  isRequired: boolean;
  options?: string[];
}

function QuestionForm() {
  const [formData, setFormData] = useState<QuestionFormData>({
    type: 'Text',
    questionText: '',
    isRequired: false,
  });

  const handleContentChange = (text: string, media?: MediaContentDto) => {
    setFormData({
      ...formData,
      questionText: text,
      mediaContent: media,
    });
  };

  const handleSubmit = async () => {
    // Submit both text and media to backend
    await questionService.createQuestion({
      ...formData,
      // Backend expects QuestionText and MediaContent separately
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      <RichTextEditor
        value={formData.questionText}
        onChange={handleContentChange}
        placeholder="Enter your question..."
        mediaType="image"
      />

      {/* Other form fields */}

      <Button type="submit">Save Question</Button>
    </form>
  );
}
```

## Browser Support

- Chrome/Edge: Full support
- Firefox: Full support
- Safari: Full support
- Mobile browsers: Touch-friendly, responsive

## Troubleshooting

### Quill not loading

Ensure CSS is imported:
```typescript
import 'react-quill/dist/quill.snow.css';
```

### Media button not appearing

Check that `readOnly` is `false` and toolbar is configured correctly.

### Upload errors

Check network tab for API errors. Verify:
1. Backend API is running
2. JWT token is valid
3. File size is within limits
4. File type is accepted

### Type errors

Ensure `MediaContentDto` and `MediaItemDto` are imported from `@/types/media`.

## Future Enhancements

- Drag-and-drop media reordering
- Inline media insertion in text
- Media caption editing
- Undo/redo support
- Custom toolbar themes
- Markdown export

## Related Components

- `MediaPicker`: Handles file uploads
- `MediaPreview`: Displays media previews
- `QuestionForm`: Uses RichTextEditor for survey questions
- `QuestionCard`: Displays questions in read-only mode

## References

- [ReactQuill Documentation](https://github.com/zenoamaro/react-quill)
- [Quill Editor API](https://quilljs.com/docs/api/)
- [Material-UI Components](https://mui.com/material-ui/getting-started/)
