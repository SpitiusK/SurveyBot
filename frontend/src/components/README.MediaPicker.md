# MediaPicker Component Documentation

## Overview

The `MediaPicker` component provides a comprehensive interface for uploading media files with drag-and-drop support, progress tracking, validation, and accessibility features.

## Features

- Drag-and-drop interface with visual feedback
- File browser fallback for traditional file selection
- Client-side file validation (type and size)
- Real-time upload progress tracking
- Preview of uploaded media
- Success/error notifications
- Accessibility support (ARIA labels, keyboard navigation)
- Responsive design with Material-UI

## Installation

The component is already installed with the required dependencies:
- `react-dropzone@14.3.8` - Drag-and-drop functionality
- `@mui/material@6.5.0` - UI components
- `@mui/icons-material@6.5.0` - Icons

## Basic Usage

```tsx
import { MediaPicker } from '@/components/MediaPicker';
import type { MediaItemDto } from '@/types/media';

function MyComponent() {
  const handleMediaSelected = (media: MediaItemDto) => {
    console.log('Uploaded media:', media);
    // Use media.id to reference the file in your question/survey
  };

  const handleError = (error: string) => {
    console.error('Upload failed:', error);
    // Show error notification to user
  };

  return (
    <MediaPicker
      mediaType="image"
      onMediaSelected={handleMediaSelected}
      onError={handleError}
    />
  );
}
```

## Props

### MediaPickerProps

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `mediaType` | `'image' \| 'video' \| 'audio' \| 'document'` | Yes | Type of media to upload |
| `onMediaSelected` | `(media: MediaItemDto) => void` | Yes | Callback when upload succeeds |
| `onError` | `(error: string) => void` | No | Callback when upload fails |
| `disabled` | `boolean` | No | Disables the picker (default: false) |

## Media Types and Limits

### Image
- **Formats**: JPG, JPEG, PNG, GIF, WebP
- **Max Size**: 10 MB
- **Use Case**: Question images, survey banners

### Video
- **Formats**: MP4, WebM, MOV
- **Max Size**: 50 MB
- **Use Case**: Video questions, tutorials

### Audio
- **Formats**: MP3, WAV, OGG, M4A
- **Max Size**: 20 MB
- **Use Case**: Audio questions, voice prompts

### Document
- **Formats**: PDF, DOC, DOCX, TXT
- **Max Size**: 25 MB
- **Use Case**: Reference documents, instructions

## API Integration

The component uploads files to:
```
POST /api/media/upload?mediaType={type}
```

### Request
- **Method**: POST
- **Headers**: `Authorization: Bearer {token}`
- **Body**: FormData with `file` field
- **Query Param**: `mediaType` (image/video/audio/document)

### Response (201 Created)
```json
{
  "success": true,
  "message": "Media uploaded successfully",
  "data": {
    "id": "uuid",
    "type": "image",
    "filePath": "/uploads/images/filename.jpg",
    "displayName": "filename.jpg",
    "fileSize": 1024567,
    "mimeType": "image/jpeg",
    "uploadedAt": "2025-11-19T10:30:00Z",
    "thumbnailPath": "/uploads/thumbnails/filename_thumb.jpg",
    "order": 0
  }
}
```

### Error Responses

**400 Bad Request** - Validation error
```json
{
  "success": false,
  "message": "File type not supported"
}
```

**401 Unauthorized** - Authentication required
```json
{
  "success": false,
  "message": "Authentication required"
}
```

**413 Payload Too Large** - File too large
```json
{
  "success": false,
  "message": "File size exceeds limit"
}
```

## Validation

### Client-Side Validation

The component validates files before upload:

1. **File Type**: Checks MIME type against accepted types
2. **File Size**: Checks against type-specific limits
3. **Error Display**: Shows validation errors in Alert component

### Server-Side Validation

Backend API should validate:
1. File type/MIME type
2. File size
3. File content (virus scan, magic bytes)
4. User authentication and authorization

## Upload Progress

The component shows real-time progress:

```tsx
interface UploadProgress {
  isUploading: boolean;
  progress: number; // 0-100
  fileName?: string;
  error?: string;
}
```

Progress is tracked using XMLHttpRequest with progress events.

## Accessibility

### ARIA Labels
- Dropzone has `role="button"` and descriptive `aria-label`
- Upload state communicated via `aria-disabled`
- Error messages have `role="alert"`

### Keyboard Navigation
- Tab to focus dropzone
- Enter/Space to open file browser
- Proper focus management during upload

### Screen Reader Support
- Progress announcements during upload
- Success/error announcements
- File information read aloud

## Integration Examples

### With React Hook Form

```tsx
import { useForm, Controller } from 'react-hook-form';
import { MediaPicker } from '@/components/MediaPicker';

interface FormData {
  questionText: string;
  image?: MediaItemDto;
}

function QuestionForm() {
  const { control, handleSubmit } = useForm<FormData>();

  const onSubmit = (data: FormData) => {
    console.log('Image ID:', data.image?.id);
    // Send data.image.id to backend
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Controller
        name="image"
        control={control}
        render={({ field }) => (
          <MediaPicker
            mediaType="image"
            onMediaSelected={field.onChange}
            onError={(error) => console.error(error)}
          />
        )}
      />
    </form>
  );
}
```

### With State Management

```tsx
function SurveyBuilder() {
  const [questionImage, setQuestionImage] = useState<MediaItemDto | null>(null);

  const handleUpload = (media: MediaItemDto) => {
    setQuestionImage(media);
    // Save to draft, update question object, etc.
  };

  return (
    <MediaPicker
      mediaType="image"
      onMediaSelected={handleUpload}
      onError={(error) => {
        // Show toast notification
        enqueueSnackbar(error, { variant: 'error' });
      }}
    />
  );
}
```

### Multiple Media Items

```tsx
function MultipleMediaUpload() {
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);

  const handleUpload = (media: MediaItemDto) => {
    setMediaItems((prev) => [...prev, media]);
  };

  return (
    <div>
      <MediaPicker
        mediaType="image"
        onMediaSelected={handleUpload}
      />
      <ul>
        {mediaItems.map((item) => (
          <li key={item.id}>{item.displayName}</li>
        ))}
      </ul>
    </div>
  );
}
```

## Styling

The component uses Material-UI theming and can be customized:

```tsx
// Wrap in ThemeProvider to customize
import { ThemeProvider, createTheme } from '@mui/material/styles';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <MediaPicker mediaType="image" {...props} />
    </ThemeProvider>
  );
}
```

## Error Handling

The component handles various error scenarios:

1. **Validation Errors**: Shown immediately in Alert
2. **Network Errors**: Caught and displayed
3. **Server Errors**: Parsed from response
4. **Cancelled Uploads**: User can cancel via close button (future)

```tsx
const handleError = (error: string) => {
  // Log error
  console.error('Upload failed:', error);

  // Show user-friendly notification
  if (error.includes('size')) {
    showNotification('File is too large. Please choose a smaller file.');
  } else if (error.includes('type')) {
    showNotification('File type not supported.');
  } else {
    showNotification('Upload failed. Please try again.');
  }
};
```

## File Locations

- **Component**: `frontend/src/components/MediaPicker.tsx`
- **Preview**: `frontend/src/components/MediaPreview.tsx`
- **Types**: `frontend/src/types/media.ts`
- **Examples**: `frontend/src/components/MediaPicker.example.tsx`

## Related Components

- `MediaPreview` - Displays uploaded media with icon/thumbnail
- `RichTextEditor` - Will integrate MediaPicker for inline media

## Next Steps

1. Integrate into RichTextEditor component (TASK-MM-016)
2. Add cancel upload functionality
3. Add image cropping/editing
4. Add multi-file upload support
5. Add drag-to-reorder for multiple items

## Support

For issues or questions:
1. Check type definitions in `types/media.ts`
2. Review examples in `MediaPicker.example.tsx`
3. Check API endpoint documentation
4. Verify file size/type constraints
