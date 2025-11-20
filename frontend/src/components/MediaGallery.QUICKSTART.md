# MediaGallery - Quick Start Guide

Get started with MediaGallery in 2 minutes.

## 1. Import

```typescript
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto } from './types/media';
```

## 2. Add State

```typescript
const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);
```

## 3. Add Handlers

```typescript
const handleAddMedia = (media: MediaItemDto) => {
  setMediaItems([...mediaItems, { ...media, order: mediaItems.length }]);
};

const handleRemoveMedia = (mediaId: string) => {
  const filtered = mediaItems.filter(m => m.id !== mediaId);
  filtered.forEach((m, i) => m.order = i);
  setMediaItems(filtered);
};

const handleReorderMedia = (items: MediaItemDto[]) => {
  setMediaItems(items);
};
```

## 4. Add Component

```typescript
<MediaGallery
  mediaItems={mediaItems}
  onAddMedia={handleAddMedia}
  onRemoveMedia={handleRemoveMedia}
  onReorderMedia={handleReorderMedia}
  mediaType="image"
/>
```

## Complete Example

```typescript
import React, { useState } from 'react';
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto } from './types/media';
import { Box, Button } from '@mui/material';

export const MyForm = () => {
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);

  const handleAddMedia = (media: MediaItemDto) => {
    setMediaItems([...mediaItems, { ...media, order: mediaItems.length }]);
  };

  const handleRemoveMedia = (mediaId: string) => {
    const filtered = mediaItems.filter(m => m.id !== mediaId);
    filtered.forEach((m, i) => m.order = i);
    setMediaItems(filtered);
  };

  const handleReorderMedia = (items: MediaItemDto[]) => {
    setMediaItems(items);
  };

  const handleSubmit = () => {
    console.log('Submitting with media:', mediaItems);
    // Your submit logic
  };

  return (
    <Box>
      <MediaGallery
        mediaItems={mediaItems}
        onAddMedia={handleAddMedia}
        onRemoveMedia={handleRemoveMedia}
        onReorderMedia={handleReorderMedia}
        mediaType="image"
      />

      <Button onClick={handleSubmit} variant="contained" sx={{ mt: 2 }}>
        Submit
      </Button>
    </Box>
  );
};
```

## Props Reference

| Prop | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `mediaItems` | `MediaItemDto[]` | Yes | - | Media items array |
| `onAddMedia` | `function` | No | - | Called when media added |
| `onRemoveMedia` | `function` | No | - | Called when media deleted |
| `onReorderMedia` | `function` | No | - | Called when reordered |
| `mediaType` | `'image'\|'video'\|'audio'\|'document'` | No | `'image'` | Type of media |
| `readOnly` | `boolean` | No | `false` | Disable editing |

## Media Types

**Image Gallery**:
```typescript
<MediaGallery ... mediaType="image" />
```

**Video Gallery**:
```typescript
<MediaGallery ... mediaType="video" />
```

**Audio Gallery**:
```typescript
<MediaGallery ... mediaType="audio" />
```

**Document Gallery**:
```typescript
<MediaGallery ... mediaType="document" />
```

## Read-Only Mode

To display without edit controls:

```typescript
<MediaGallery
  mediaItems={mediaItems}
  readOnly={true}
  mediaType="image"
/>
```

## Features

- Drag-and-drop to reorder
- Click "Add Media" to upload
- Hover to reveal delete button
- Responsive grid layout
- Empty state with CTA
- Type-specific icons

## Need More?

- **Full Docs**: See `MediaGallery.README.md`
- **Examples**: See `MediaGallery.example.tsx`
- **Integration**: See `MediaGallery.integration.md`

---

That's it! You're ready to use MediaGallery.
