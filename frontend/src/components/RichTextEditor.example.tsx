/**
 * RichTextEditor Usage Examples
 *
 * This file demonstrates various ways to use the RichTextEditor component
 * in different scenarios.
 */

import React, { useState, useMemo } from 'react';
import { RichTextEditor } from './RichTextEditor';
import type { MediaContentDto, MediaItemDto } from '../types/media';
import {
  Box,
  Button,
  Card,
  CardContent,
  Typography,
  Alert,
  Stack,
  Divider,
} from '@mui/material';

// Example 1: Basic Usage
export function BasicExample() {
  const [content, setContent] = useState('');
  const [media, setMedia] = useState<MediaContentDto>();

  const handleChange = (text: string, mediaContent?: MediaContentDto) => {
    setContent(text);
    setMedia(mediaContent);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Basic Rich Text Editor
      </Typography>

      <RichTextEditor
        value={content}
        onChange={handleChange}
        placeholder="Start typing your question..."
        mediaType="image"
      />

      {/* Display current state */}
      <Box sx={{ mt: 3, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
        <Typography variant="subtitle2">Current Content:</Typography>
        <pre style={{ fontSize: '12px', overflow: 'auto' }}>
          {JSON.stringify({ text: content, media }, null, 2)}
        </pre>
      </Box>
    </Box>
  );
}

// Example 2: With Form Integration
export function FormExample() {
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    questionText: '',
    mediaContent: undefined as MediaContentDto | undefined,
  });

  const handleContentChange = (text: string, media?: MediaContentDto) => {
    setFormData({
      ...formData,
      questionText: text,
      mediaContent: media,
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Form submitted:', formData);
    alert('Form data logged to console');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Question Form with Rich Text
      </Typography>

      <form onSubmit={handleSubmit}>
        <Stack spacing={3}>
          <RichTextEditor
            value={formData.questionText}
            onChange={handleContentChange}
            placeholder="Enter your survey question..."
            mediaType="image"
          />

          <Box sx={{ display: 'flex', gap: 2 }}>
            <Button variant="contained" type="submit">
              Save Question
            </Button>
            <Button
              variant="outlined"
              onClick={() =>
                setFormData({
                  title: '',
                  description: '',
                  questionText: '',
                  mediaContent: undefined,
                })
              }
            >
              Reset
            </Button>
          </Box>
        </Stack>
      </form>
    </Box>
  );
}

// Example 3: With Initial Media
export function WithInitialMediaExample() {
  const initialMedia: MediaItemDto[] = useMemo(
    () => [
      {
        id: 'media-1',
        type: 'image',
        filePath: '/uploads/sample.jpg',
        thumbnailPath: '/uploads/sample_thumb.jpg',
        displayName: 'Sample Image.jpg',
        fileSize: 2048000,
        mimeType: 'image/jpeg',
        uploadedAt: new Date().toISOString(),
        order: 0,
      },
    ],
    []
  );

  const [content, setContent] = useState(
    '<h2>Sample Question</h2><p>This question has a pre-loaded image attachment.</p>'
  );

  const handleChange = (text: string, _media?: MediaContentDto) => {
    setContent(text);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Editor with Pre-loaded Media
      </Typography>

      <RichTextEditor
        value={content}
        onChange={handleChange}
        initialMedia={initialMedia}
        mediaType="image"
      />
    </Box>
  );
}

// Example 4: Read-Only Mode
export function ReadOnlyExample() {
  const content =
    '<h2>Survey Question Preview</h2><p>This is how your question will appear to respondents.</p><ul><li>Point one</li><li>Point two</li><li>Point three</li></ul>';

  const mediaItems: MediaItemDto[] = [
    {
      id: 'media-1',
      type: 'image',
      filePath: '/uploads/question-image.jpg',
      thumbnailPath: '/uploads/question-image_thumb.jpg',
      displayName: 'Reference Image.jpg',
      fileSize: 1536000,
      mimeType: 'image/jpeg',
      uploadedAt: new Date().toISOString(),
      order: 0,
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Read-Only Question Display
      </Typography>

      <Card>
        <CardContent>
          <RichTextEditor
            value={content}
            onChange={() => {}} // No-op
            readOnly={true}
            initialMedia={mediaItems}
          />
        </CardContent>
      </Card>
    </Box>
  );
}

// Example 5: Different Media Types
export function MediaTypesExample() {
  const [selectedType, setSelectedType] = useState<
    'image' | 'video' | 'audio' | 'document'
  >('image');
  const [content, setContent] = useState('');

  const handleChange = (text: string, _media?: MediaContentDto) => {
    setContent(text);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Different Media Types
      </Typography>

      <Stack spacing={2}>
        {/* Media Type Selector */}
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant={selectedType === 'image' ? 'contained' : 'outlined'}
            onClick={() => setSelectedType('image')}
          >
            Image
          </Button>
          <Button
            variant={selectedType === 'video' ? 'contained' : 'outlined'}
            onClick={() => setSelectedType('video')}
          >
            Video
          </Button>
          <Button
            variant={selectedType === 'audio' ? 'contained' : 'outlined'}
            onClick={() => setSelectedType('audio')}
          >
            Audio
          </Button>
          <Button
            variant={selectedType === 'document' ? 'contained' : 'outlined'}
            onClick={() => setSelectedType('document')}
          >
            Document
          </Button>
        </Box>

        <RichTextEditor
          value={content}
          onChange={handleChange}
          placeholder={`Question with ${selectedType} upload...`}
          mediaType={selectedType}
        />
      </Stack>
    </Box>
  );
}

// Example 6: With Error Handling
export function ErrorHandlingExample() {
  const [content, setContent] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleChange = (text: string, _media?: MediaContentDto) => {
    setContent(text);
    setError(null); // Clear error on successful change
  };

  const handleError = (errorMessage: string) => {
    setError(errorMessage);
    console.error('Upload error:', errorMessage);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Editor with Error Handling
      </Typography>

      <Stack spacing={2}>
        {error && (
          <Alert severity="error" onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <RichTextEditor
          value={content}
          onChange={handleChange}
          onError={handleError}
          placeholder="Try uploading an invalid file to see error handling..."
          mediaType="image"
        />
      </Stack>
    </Box>
  );
}

// Example 7: All Examples in One Page
export function AllExamples() {
  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h3" gutterBottom>
        RichTextEditor Component Examples
      </Typography>
      <Typography variant="body1" color="text.secondary" paragraph>
        Comprehensive examples demonstrating various use cases of the
        RichTextEditor component.
      </Typography>

      <Divider sx={{ my: 4 }} />

      <Stack spacing={6} divider={<Divider />}>
        <BasicExample />
        <FormExample />
        <WithInitialMediaExample />
        <ReadOnlyExample />
        <MediaTypesExample />
        <ErrorHandlingExample />
      </Stack>
    </Box>
  );
}

// Example 8: Advanced - Auto-save with Debouncing
export function AutoSaveExample() {
  const [content, setContent] = useState('');
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  // Simulate auto-save
  const saveToBackend = async (text: string, media?: MediaContentDto) => {
    setIsSaving(true);
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 1000));
    console.log('Saved:', { text, media });
    setLastSaved(new Date());
    setIsSaving(false);
  };

  // Debounced save (simplified - in real app use lodash debounce)
  const handleChange = (text: string, media?: MediaContentDto) => {
    setContent(text);
    // In real app, this should be debounced
    saveToBackend(text, media);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 2,
        }}
      >
        <Typography variant="h5">Auto-Save Editor</Typography>
        <Typography variant="caption" color="text.secondary">
          {isSaving
            ? 'Saving...'
            : lastSaved
            ? `Last saved: ${lastSaved.toLocaleTimeString()}`
            : 'Not saved yet'}
        </Typography>
      </Box>

      <RichTextEditor
        value={content}
        onChange={handleChange}
        placeholder="Your changes are automatically saved..."
        mediaType="image"
      />
    </Box>
  );
}

export default AllExamples;
