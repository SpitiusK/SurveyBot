# RichTextEditor - Quick Start Guide

**5-Minute Integration Guide for QuestionForm**

---

## Step 1: Import the Component

```typescript
import { RichTextEditor } from '@/components/RichTextEditor';
import type { MediaContentDto } from '@/types/media';
import 'react-quill/dist/quill.snow.css'; // Required CSS
```

## Step 2: Add to Your Component

### Option A: Simple State (No Form Library)

```typescript
function QuestionForm() {
  const [questionText, setQuestionText] = useState('');
  const [mediaContent, setMediaContent] = useState<MediaContentDto>();

  const handleContentChange = (text: string, media?: MediaContentDto) => {
    setQuestionText(text);
    setMediaContent(media);
  };

  const handleSubmit = async () => {
    await questionService.createQuestion({
      questionText,
      mediaContent,
      type: 'Text',
      isRequired: true,
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      <RichTextEditor
        value={questionText}
        onChange={handleContentChange}
        placeholder="Enter your question..."
        mediaType="image"
      />
      <button type="submit">Save Question</button>
    </form>
  );
}
```

### Option B: React Hook Form Integration

```typescript
function QuestionForm() {
  const { control, handleSubmit, setValue } = useForm<QuestionFormData>();

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
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
      <button type="submit">Save</button>
    </form>
  );
}
```

## Step 3: Update Form Data Type

```typescript
interface QuestionFormData {
  questionText: string;
  mediaContent?: MediaContentDto;
  type: 'Text' | 'SingleChoice' | 'MultipleChoice' | 'Rating';
  isRequired: boolean;
  order: number;
}
```

## Step 4: Handle API Submission

```typescript
const onSubmit = async (data: QuestionFormData) => {
  try {
    const response = await questionService.createQuestion({
      questionText: data.questionText,
      mediaContent: data.mediaContent,
      type: data.type,
      isRequired: data.isRequired,
      order: data.order,
    });

    console.log('Question created:', response);
    // Navigate or show success message
  } catch (error) {
    console.error('Error creating question:', error);
    // Show error message
  }
};
```

## Step 5: Handle Edit Mode (Optional)

```typescript
// Load existing question
useEffect(() => {
  if (questionId) {
    loadQuestion(questionId);
  }
}, [questionId]);

const loadQuestion = async (id: number) => {
  const question = await questionService.getQuestion(id);

  setQuestionText(question.questionText);
  setMediaContent(question.mediaContent);

  // Or with React Hook Form:
  setValue('questionText', question.questionText);
  setValue('mediaContent', question.mediaContent);
};

// Render with initial media
<RichTextEditor
  value={questionText}
  onChange={handleContentChange}
  initialMedia={mediaContent?.items || []}
  mediaType="image"
/>
```

## Step 6: Add Error Handling

```typescript
const [error, setError] = useState<string | null>(null);

<RichTextEditor
  value={questionText}
  onChange={handleContentChange}
  onError={(errorMessage) => {
    setError(errorMessage);
    console.error('Upload error:', errorMessage);
  }}
/>

{error && (
  <Alert severity="error" onClose={() => setError(null)}>
    {error}
  </Alert>
)}
```

## Common Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `string` | Required | HTML content |
| `onChange` | `(text, media?) => void` | Required | Change handler |
| `placeholder` | `string` | `'Enter question text...'` | Placeholder |
| `mediaType` | `'image' \| 'video' \| 'audio' \| 'document'` | `'image'` | Upload type |
| `initialMedia` | `MediaItemDto[]` | `[]` | Pre-loaded media |
| `readOnly` | `boolean` | `false` | Read-only mode |
| `onError` | `(error: string) => void` | Optional | Error handler |

## Media Types

```typescript
// Image questions
<RichTextEditor mediaType="image" {...props} />

// Video questions
<RichTextEditor mediaType="video" {...props} />

// Audio questions
<RichTextEditor mediaType="audio" {...props} />

// Document questions
<RichTextEditor mediaType="document" {...props} />
```

## Read-Only Mode (Display Questions)

```typescript
<RichTextEditor
  value={question.questionText}
  onChange={() => {}} // No-op
  readOnly={true}
  initialMedia={question.mediaContent?.items || []}
/>
```

## Styling

The component uses Material-UI and is styled via the `sx` prop:

```typescript
<Box sx={{ '& .ql-editor': { minHeight: '400px' } }}>
  <RichTextEditor {...props} />
</Box>
```

## Full Example (Complete QuestionForm)

```typescript
import React, { useState } from 'react';
import { RichTextEditor } from '@/components/RichTextEditor';
import type { MediaContentDto } from '@/types/media';
import { Box, Button, TextField, Alert } from '@mui/material';
import 'react-quill/dist/quill.snow.css';

export function QuestionForm() {
  const [questionText, setQuestionText] = useState('');
  const [mediaContent, setMediaContent] = useState<MediaContentDto>();
  const [isRequired, setIsRequired] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleContentChange = (text: string, media?: MediaContentDto) => {
    setQuestionText(text);
    setMediaContent(media);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      setLoading(true);
      setError(null);

      await questionService.createQuestion({
        questionText,
        mediaContent,
        type: 'Text',
        isRequired,
        order: 0,
      });

      // Success - reset form or navigate
      setQuestionText('');
      setMediaContent(undefined);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save question');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ p: 3 }}>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <RichTextEditor
        value={questionText}
        onChange={handleContentChange}
        onError={setError}
        placeholder="Enter your question..."
        mediaType="image"
      />

      <Box sx={{ mt: 3, display: 'flex', gap: 2, alignItems: 'center' }}>
        <label>
          <input
            type="checkbox"
            checked={isRequired}
            onChange={(e) => setIsRequired(e.target.checked)}
          />
          Required
        </label>

        <Button type="submit" variant="contained" disabled={loading}>
          {loading ? 'Saving...' : 'Save Question'}
        </Button>

        <Button
          variant="outlined"
          onClick={() => {
            setQuestionText('');
            setMediaContent(undefined);
            setError(null);
          }}
        >
          Reset
        </Button>
      </Box>
    </Box>
  );
}
```

## Troubleshooting

### CSS not loading
```typescript
// Add to top of file
import 'react-quill/dist/quill.snow.css';
```

### TypeScript errors
```typescript
// Use type-only imports
import type { MediaContentDto } from '@/types/media';
```

### Upload not working
```typescript
// Check MediaPicker is configured correctly
// Verify API endpoint is accessible
// Check JWT token in localStorage
```

## Next Steps

1. Copy the basic example above
2. Adjust form fields as needed
3. Connect to your API service
4. Test upload and save
5. Add validation if needed

For more details, see:
- Full documentation: `RichTextEditor.md`
- Advanced examples: `RichTextEditor.example.tsx`
- Test cases: `RichTextEditor.test.tsx`

---

**You're ready to go!** The RichTextEditor is production-ready and fully integrated with the MediaPicker system.
