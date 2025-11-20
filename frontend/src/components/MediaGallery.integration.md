# MediaGallery Integration Guide

Quick guide for integrating MediaGallery into QuestionForm or other components.

## Quick Integration in QuestionForm

### Step 1: Import Components

```typescript
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto, MediaType } from './types/media';
```

### Step 2: Add State

```typescript
const QuestionForm = () => {
  const [questionText, setQuestionText] = useState('');
  const [questionType, setQuestionType] = useState('text');

  // Add media state
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);

  // ... other state
};
```

### Step 3: Add Event Handlers

```typescript
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
```

### Step 4: Add to Form JSX

```typescript
return (
  <form onSubmit={handleSubmit}>
    {/* Existing form fields */}
    <TextField
      label="Question Text"
      value={questionText}
      onChange={(e) => setQuestionText(e.target.value)}
      fullWidth
    />

    {/* Add MediaGallery */}
    <Box sx={{ mt: 3 }}>
      <Typography variant="subtitle1" gutterBottom>
        Attachments
      </Typography>
      <MediaGallery
        mediaItems={mediaItems}
        onAddMedia={handleAddMedia}
        onRemoveMedia={handleRemoveMedia}
        onReorderMedia={handleReorderMedia}
        mediaType="image"
      />
    </Box>

    {/* Submit button */}
    <Button type="submit" variant="contained">
      Save Question
    </Button>
  </form>
);
```

### Step 5: Include in Form Submission

```typescript
const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();

  const questionData = {
    text: questionText,
    type: questionType,
    // Include media items
    media: mediaItems,
  };

  await saveQuestion(questionData);
};
```

## Complete Example

```typescript
import React, { useState } from 'react';
import { MediaGallery } from './components/MediaGallery';
import type { MediaItemDto } from './types/media';
import {
  Box,
  TextField,
  Button,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';

interface QuestionFormProps {
  onSubmit: (data: QuestionData) => void;
  initialData?: QuestionData;
}

interface QuestionData {
  text: string;
  type: string;
  media: MediaItemDto[];
}

export const QuestionForm: React.FC<QuestionFormProps> = ({
  onSubmit,
  initialData,
}) => {
  const [questionText, setQuestionText] = useState(initialData?.text || '');
  const [questionType, setQuestionType] = useState(initialData?.type || 'text');
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>(
    initialData?.media || []
  );

  const handleAddMedia = (media: MediaItemDto) => {
    setMediaItems([...mediaItems, { ...media, order: mediaItems.length }]);
  };

  const handleRemoveMedia = (mediaId: string) => {
    const filtered = mediaItems.filter((m) => m.id !== mediaId);
    filtered.forEach((m, i) => (m.order = i));
    setMediaItems(filtered);
  };

  const handleReorderMedia = (reordered: MediaItemDto[]) => {
    setMediaItems(reordered);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      text: questionText,
      type: questionType,
      media: mediaItems,
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        {/* Question Text */}
        <TextField
          label="Question Text"
          value={questionText}
          onChange={(e) => setQuestionText(e.target.value)}
          required
          fullWidth
          multiline
          rows={3}
        />

        {/* Question Type */}
        <FormControl fullWidth>
          <InputLabel>Question Type</InputLabel>
          <Select
            value={questionType}
            onChange={(e) => setQuestionType(e.target.value)}
            label="Question Type"
          >
            <MenuItem value="text">Text</MenuItem>
            <MenuItem value="single_choice">Single Choice</MenuItem>
            <MenuItem value="multiple_choice">Multiple Choice</MenuItem>
            <MenuItem value="rating">Rating</MenuItem>
          </Select>
        </FormControl>

        {/* Media Gallery */}
        <Box>
          <Typography variant="subtitle1" gutterBottom>
            Media Attachments (Optional)
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            Add images, videos, audio, or documents to enhance your question
          </Typography>
          <MediaGallery
            mediaItems={mediaItems}
            onAddMedia={handleAddMedia}
            onRemoveMedia={handleRemoveMedia}
            onReorderMedia={handleReorderMedia}
            mediaType="image"
          />
        </Box>

        {/* Submit */}
        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
          <Button type="button" variant="outlined">
            Cancel
          </Button>
          <Button type="submit" variant="contained">
            Save Question
          </Button>
        </Box>
      </Box>
    </form>
  );
};
```

## With React Hook Form

If using React Hook Form for form management:

```typescript
import { useForm, Controller } from 'react-hook-form';
import { MediaGallery } from './components/MediaGallery';

const QuestionForm = () => {
  const { control, handleSubmit, watch } = useForm({
    defaultValues: {
      text: '',
      type: 'text',
      media: [],
    },
  });

  const mediaItems = watch('media');

  const onSubmit = (data) => {
    console.log('Form data:', data);
    // Submit to API
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {/* Text field with Controller */}
      <Controller
        name="text"
        control={control}
        rules={{ required: 'Question text is required' }}
        render={({ field, fieldState: { error } }) => (
          <TextField
            {...field}
            label="Question Text"
            error={!!error}
            helperText={error?.message}
            fullWidth
          />
        )}
      />

      {/* Media Gallery with Controller */}
      <Controller
        name="media"
        control={control}
        render={({ field: { value, onChange } }) => (
          <MediaGallery
            mediaItems={value}
            onAddMedia={(media) => onChange([...value, media])}
            onRemoveMedia={(id) =>
              onChange(value.filter((m) => m.id !== id))
            }
            onReorderMedia={onChange}
            mediaType="image"
          />
        )}
      />

      <Button type="submit">Save</Button>
    </form>
  );
};
```

## Different Media Types

### Image Gallery

```typescript
<MediaGallery
  mediaItems={imageItems}
  onAddMedia={handleAddImage}
  onRemoveMedia={handleRemoveImage}
  onReorderMedia={handleReorderImages}
  mediaType="image"
/>
```

### Video Gallery

```typescript
<MediaGallery
  mediaItems={videoItems}
  onAddMedia={handleAddVideo}
  onRemoveMedia={handleRemoveVideo}
  onReorderMedia={handleReorderVideos}
  mediaType="video"
/>
```

### Audio Gallery

```typescript
<MediaGallery
  mediaItems={audioItems}
  onAddMedia={handleAddAudio}
  onRemoveMedia={handleRemoveAudio}
  onReorderMedia={handleReorderAudio}
  mediaType="audio"
/>
```

### Document Gallery

```typescript
<MediaGallery
  mediaItems={documentItems}
  onAddMedia={handleAddDocument}
  onRemoveMedia={handleRemoveDocument}
  onReorderMedia={handleReorderDocuments}
  mediaType="document"
/>
```

## Tips

1. **Order Management**: Always update the `order` property when adding, removing, or reordering
2. **Unique IDs**: Ensure each media item has a unique `id`
3. **Read-Only Mode**: Use `readOnly={true}` for displaying submitted surveys
4. **Error Handling**: Add error boundaries around the component
5. **Loading States**: Show loading spinner while fetching media
6. **Validation**: Validate media items before form submission

## Common Patterns

### Loading Existing Media

```typescript
useEffect(() => {
  const loadMedia = async () => {
    const media = await fetchQuestionMedia(questionId);
    setMediaItems(media);
  };
  loadMedia();
}, [questionId]);
```

### Conditional Display

```typescript
{questionType === 'text' && (
  <MediaGallery
    mediaItems={mediaItems}
    onAddMedia={handleAddMedia}
    onRemoveMedia={handleRemoveMedia}
    onReorderMedia={handleReorderMedia}
    mediaType="image"
  />
)}
```

### Multiple Galleries

```typescript
<Box>
  <Typography variant="h6">Images</Typography>
  <MediaGallery
    mediaItems={images}
    onAddMedia={handleAddImage}
    onRemoveMedia={handleRemoveImage}
    onReorderMedia={handleReorderImages}
    mediaType="image"
  />

  <Typography variant="h6" sx={{ mt: 3 }}>Videos</Typography>
  <MediaGallery
    mediaItems={videos}
    onAddMedia={handleAddVideo}
    onRemoveMedia={handleRemoveVideo}
    onReorderMedia={handleReorderVideos}
    mediaType="video"
  />
</Box>
```

## Testing

Test the integration:

```typescript
describe('QuestionForm with MediaGallery', () => {
  it('should add media to form', async () => {
    const { getByText } = render(<QuestionForm />);

    // Click add media button
    fireEvent.click(getByText('Add Media'));

    // Upload file
    // ... upload logic

    // Verify media appears in gallery
    expect(getByText('image.jpg')).toBeInTheDocument();
  });

  it('should remove media from form', async () => {
    // ... test deletion
  });

  it('should reorder media', async () => {
    // ... test drag-and-drop
  });
});
```

---

**Ready to use!** The MediaGallery component is fully integrated and ready for use in your QuestionForm.
