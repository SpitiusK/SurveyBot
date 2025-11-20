# MediaGallery Component

A comprehensive React component for displaying and managing media attachments in a grid layout with drag-and-drop reordering, add/remove functionality, and responsive design.

## Features

- **Responsive Grid Layout**: Automatically adjusts from 1 column (mobile) to 3 columns (desktop)
- **Drag-and-Drop Reordering**: Intuitive drag-and-drop interface for reordering media items
- **Add Media**: Integrated MediaPicker for uploading new media
- **Delete Media**: Remove media with confirmation dialog
- **Read-Only Mode**: Display-only view without edit controls
- **Empty State**: User-friendly empty state with call-to-action
- **Type-Specific Icons**: Visual indicators for images, videos, audio, and documents
- **File Information**: Display file name, size, type, and alt text
- **Accessibility**: Keyboard navigation, ARIA labels, and focus management

## Installation

The component is already part of your project. Import it as follows:

```typescript
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto, MediaType } from './types/media';
```

## Props

| Prop | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `mediaItems` | `MediaItemDto[]` | Yes | `[]` | Array of media items to display |
| `onAddMedia` | `(media: MediaItemDto) => void` | No | - | Callback when new media is added |
| `onRemoveMedia` | `(mediaId: string) => void` | No | - | Callback when media is deleted |
| `onReorderMedia` | `(items: MediaItemDto[]) => void` | No | - | Callback when media is reordered |
| `readOnly` | `boolean` | No | `false` | If true, disables add/delete/reorder |
| `mediaType` | `MediaType` | No | `'image'` | Type of media: `'image' \| 'video' \| 'audio' \| 'document'` |

## MediaItemDto Interface

```typescript
interface MediaItemDto {
  id: string;
  type: MediaType;
  filePath: string;
  displayName: string;
  fileSize: number; // in bytes
  mimeType: string;
  uploadedAt: string; // ISO date string
  altText?: string;
  thumbnailPath?: string;
  order: number;
}
```

## Basic Usage

### Simple Gallery (Editable)

```typescript
import React, { useState } from 'react';
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto } from './types/media';

const MyForm = () => {
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);

  const handleAddMedia = (media: MediaItemDto) => {
    setMediaItems([...mediaItems, { ...media, order: mediaItems.length }]);
  };

  const handleRemoveMedia = (mediaId: string) => {
    const filtered = mediaItems.filter(m => m.id !== mediaId);
    // Update order after deletion
    filtered.forEach((m, i) => m.order = i);
    setMediaItems(filtered);
  };

  const handleReorderMedia = (reordered: MediaItemDto[]) => {
    setMediaItems(reordered);
  };

  return (
    <MediaGallery
      mediaItems={mediaItems}
      onAddMedia={handleAddMedia}
      onRemoveMedia={handleRemoveMedia}
      onReorderMedia={handleReorderMedia}
      mediaType="image"
    />
  );
};
```

### Read-Only Gallery

```typescript
<MediaGallery
  mediaItems={mediaItems}
  readOnly={true}
  mediaType="image"
/>
```

### Video Gallery

```typescript
<MediaGallery
  mediaItems={videoItems}
  onAddMedia={handleAddVideo}
  onRemoveMedia={handleRemoveVideo}
  onReorderMedia={handleReorderVideo}
  mediaType="video"
/>
```

## Advanced Usage

### Integrating with React Hook Form

```typescript
import { useForm, Controller } from 'react-hook-form';
import { MediaGallery } from './components/MediaGallery';

const QuestionForm = () => {
  const { control, watch } = useForm({
    defaultValues: {
      media: [],
    },
  });

  return (
    <form>
      <Controller
        name="media"
        control={control}
        render={({ field: { value, onChange } }) => (
          <MediaGallery
            mediaItems={value}
            onAddMedia={(media) => onChange([...value, media])}
            onRemoveMedia={(id) => onChange(value.filter(m => m.id !== id))}
            onReorderMedia={onChange}
            mediaType="image"
          />
        )}
      />
    </form>
  );
};
```

### With Custom State Management

```typescript
import { useReducer } from 'react';
import { MediaGallery } from './components/MediaGallery';

type MediaAction =
  | { type: 'ADD'; media: MediaItemDto }
  | { type: 'REMOVE'; id: string }
  | { type: 'REORDER'; items: MediaItemDto[] };

const mediaReducer = (state: MediaItemDto[], action: MediaAction) => {
  switch (action.type) {
    case 'ADD':
      return [...state, { ...action.media, order: state.length }];
    case 'REMOVE':
      const filtered = state.filter(m => m.id !== action.id);
      filtered.forEach((m, i) => m.order = i);
      return filtered;
    case 'REORDER':
      return action.items;
    default:
      return state;
  }
};

const MyComponent = () => {
  const [media, dispatch] = useReducer(mediaReducer, []);

  return (
    <MediaGallery
      mediaItems={media}
      onAddMedia={(m) => dispatch({ type: 'ADD', media: m })}
      onRemoveMedia={(id) => dispatch({ type: 'REMOVE', id })}
      onReorderMedia={(items) => dispatch({ type: 'REORDER', items })}
      mediaType="image"
    />
  );
};
```

## Component Architecture

### File Structure

```
MediaGallery/
├── MediaGallery.tsx          # Main gallery container
├── MediaGalleryItem.tsx      # Individual media card
├── MediaGallery.css          # Component styles
├── MediaGallery.example.tsx  # Usage examples
└── MediaGallery.README.md    # This file
```

### MediaGallery (Main Component)

Responsibilities:
- Grid layout management
- State for dialogs (add media, delete confirmation)
- Drag-and-drop coordination
- Empty state display

### MediaGalleryItem (Item Component)

Responsibilities:
- Individual media card display
- Thumbnail/icon rendering
- File information display
- Drag-and-drop handlers
- Delete button

## Styling

The component uses a combination of:
1. **MUI sx prop**: For theme-aware styling
2. **CSS classes**: For animations and responsive design
3. **CSS custom properties**: For theme customization

### Customization

You can customize the appearance by:

1. **Overriding CSS classes**:

```css
/* In your global CSS */
.media-gallery-item {
  border-radius: 16px;
}

.media-gallery-item:hover {
  transform: scale(1.05);
}
```

2. **Using MUI theme**:

```typescript
// In your theme.ts
const theme = createTheme({
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
      },
    },
  },
});
```

## Accessibility

The component follows accessibility best practices:

- **Keyboard Navigation**: All interactive elements are keyboard accessible
- **ARIA Labels**: Proper labels for screen readers
- **Focus Management**: Visible focus indicators
- **Semantic HTML**: Correct use of ARIA roles
- **Alt Text**: Support for image alt text

### Keyboard Shortcuts

- **Tab**: Navigate between items
- **Enter/Space**: Activate buttons
- **Escape**: Close dialogs

## Drag-and-Drop Behavior

The drag-and-drop implementation:

1. **Drag Start**: Visual feedback (opacity change)
2. **Drag Over**: Highlight drop target
3. **Drop**: Swap items and update order
4. **Drag End**: Reset visual states

### Visual Feedback

- Dragged item: 50% opacity
- Drop target: Blue border highlight
- Hover state: Elevated shadow

## Empty State

When no media items are present:
- Shows a large icon
- Displays helpful text
- Provides "Add Media" button
- Responsive layout

## Delete Confirmation

Before deleting media:
- Shows confirmation dialog
- Warns that action is irreversible
- Provides Cancel/Delete options
- Delete button is prominently styled in red

## Performance Considerations

- **Memoization**: Uses `useCallback` for handlers
- **Lazy Loading**: Thumbnails loaded on demand
- **Optimized Renders**: Only affected items re-render
- **CSS Animations**: Hardware-accelerated transforms

## Browser Support

Supports all modern browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

Note: Drag-and-drop requires HTML5 Drag and Drop API support.

## Troubleshooting

### Media not uploading

**Issue**: MediaPicker fails to upload

**Solution**: Check:
1. API endpoint is correct (`/api/media/upload`)
2. Authentication token is valid
3. File size within limits
4. File type is accepted

### Drag-and-drop not working

**Issue**: Cannot reorder items

**Solution**: Check:
1. `readOnly` prop is `false`
2. `onReorderMedia` callback is provided
3. Items have unique `id` properties
4. Browser supports HTML5 Drag API

### Items not displaying

**Issue**: Gallery shows empty

**Solution**: Check:
1. `mediaItems` prop has valid data
2. Each item has required properties
3. `thumbnailPath` exists for images
4. No console errors

### Styling issues

**Issue**: Cards look broken

**Solution**: Check:
1. `MediaGallery.css` is imported
2. MUI theme is configured
3. No conflicting global styles
4. Browser DevTools for CSS conflicts

## Related Components

- **MediaPicker**: Upload new media files
- **MediaPreview**: Display media thumbnails
- **QuestionForm**: Parent component for survey questions

## Future Enhancements

Potential improvements:
- Lightbox viewer for full-size images
- Multi-select for batch operations
- Filter by media type
- Search functionality
- Bulk upload
- Image cropping/editing
- Video thumbnail generation
- Audio waveform preview

## API Integration

The component works with the following API endpoints:

### Upload Media

```
POST /api/media/upload?mediaType=image
Content-Type: multipart/form-data
Authorization: Bearer <token>

Body: FormData with 'file' field
```

Response:
```json
{
  "success": true,
  "data": {
    "id": "media_123",
    "type": "image",
    "filePath": "/uploads/image.jpg",
    "displayName": "image.jpg",
    "fileSize": 2450000,
    "mimeType": "image/jpeg",
    "uploadedAt": "2025-11-19T10:00:00Z",
    "thumbnailPath": "/uploads/thumbnails/image.jpg",
    "order": 0
  }
}
```

### Delete Media (Optional)

If your backend supports media deletion:

```
DELETE /api/media/{mediaId}
Authorization: Bearer <token>
```

## Testing

To test the component:

1. **Manual Testing**: Use `MediaGallery.example.tsx`
2. **Unit Tests**: Test individual functions
3. **Integration Tests**: Test with parent components
4. **E2E Tests**: Test drag-and-drop and dialogs

## License

Part of SurveyBot project. See main project license.

## Support

For issues or questions:
1. Check this README
2. Review example file
3. Check console for errors
4. Verify props are correct
5. Contact development team

---

**Last Updated**: 2025-11-19
**Version**: 1.0.0
**Author**: SurveyBot Development Team
