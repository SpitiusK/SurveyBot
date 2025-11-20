/**
 * MediaPicker Component Usage Example
 *
 * This file demonstrates how to integrate the MediaPicker component
 * into your forms and pages.
 */

import React, { useState } from 'react';
import { Box, Typography, Stack, Chip } from '@mui/material';
import { MediaPicker } from './MediaPicker';
import type { MediaItemDto, MediaType } from '../types/media';

/**
 * Example 1: Basic Image Upload
 */
export function BasicImageUploadExample() {
  const [uploadedImage, setUploadedImage] = useState<MediaItemDto | null>(null);

  const handleMediaSelected = (media: MediaItemDto) => {
    console.log('Image uploaded:', media);
    setUploadedImage(media);
  };

  const handleError = (error: string) => {
    console.error('Upload error:', error);
    // You can show a toast/snackbar notification here
  };

  return (
    <Box sx={{ maxWidth: 600, mx: 'auto', p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Upload an Image
      </Typography>

      <MediaPicker
        mediaType="image"
        onMediaSelected={handleMediaSelected}
        onError={handleError}
      />

      {uploadedImage && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Uploaded: {uploadedImage.displayName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            ID: {uploadedImage.id}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

/**
 * Example 2: Multiple Media Types with Tabs
 */
export function MultipleMediaTypesExample() {
  const [selectedType, setSelectedType] = useState<MediaType>('image');
  const [uploadedMedia, setUploadedMedia] = useState<MediaItemDto[]>([]);

  const handleMediaSelected = (media: MediaItemDto) => {
    setUploadedMedia((prev) => [...prev, media]);
  };

  const handleError = (error: string) => {
    alert(`Error: ${error}`);
  };

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Upload Multiple Media Types
      </Typography>

      <Stack direction="row" spacing={1} sx={{ mb: 3 }}>
        {(['image', 'video', 'audio', 'document'] as MediaType[]).map((type) => (
          <Chip
            key={type}
            label={type}
            color={selectedType === type ? 'primary' : 'default'}
            onClick={() => setSelectedType(type)}
            sx={{ textTransform: 'capitalize' }}
          />
        ))}
      </Stack>

      <MediaPicker
        mediaType={selectedType}
        onMediaSelected={handleMediaSelected}
        onError={handleError}
      />

      <Box sx={{ mt: 3 }}>
        <Typography variant="h6" gutterBottom>
          Uploaded Media ({uploadedMedia.length})
        </Typography>
        <Stack spacing={1}>
          {uploadedMedia.map((media) => (
            <Box
              key={media.id}
              sx={{
                p: 2,
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 1,
              }}
            >
              <Typography variant="body2">
                {media.displayName} ({media.type})
              </Typography>
            </Box>
          ))}
        </Stack>
      </Box>
    </Box>
  );
}

/**
 * Example 3: Integration with React Hook Form
 */
import { useForm, Controller } from 'react-hook-form';

interface FormData {
  title: string;
  questionImage?: MediaItemDto;
}

export function FormIntegrationExample() {
  const { control, handleSubmit, setValue } = useForm<FormData>({
    defaultValues: {
      title: '',
    },
  });

  const onSubmit = (data: FormData) => {
    console.log('Form submitted:', data);
    // data.questionImage will contain the uploaded media info
  };

  const handleMediaSelected = (media: MediaItemDto) => {
    setValue('questionImage', media);
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit(onSubmit)}
      sx={{ maxWidth: 600, mx: 'auto', p: 3 }}
    >
      <Typography variant="h5" gutterBottom>
        Form with Media Upload
      </Typography>

      <Controller
        name="questionImage"
        control={control}
        render={({ field }) => (
          <MediaPicker
            mediaType="image"
            onMediaSelected={(media) => {
              field.onChange(media);
              handleMediaSelected(media);
            }}
            onError={(error) => console.error(error)}
          />
        )}
      />
    </Box>
  );
}

/**
 * Example 4: Conditional Upload (Disabled State)
 */
export function ConditionalUploadExample() {
  const [isUploading, setIsUploading] = useState(false);

  const handleMediaSelected = (media: MediaItemDto) => {
    console.log('Media uploaded:', media);
    setIsUploading(false);
  };

  return (
    <Box sx={{ maxWidth: 600, mx: 'auto', p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Disabled State Example
      </Typography>

      <MediaPicker
        mediaType="document"
        onMediaSelected={handleMediaSelected}
        onError={(error) => console.error(error)}
        disabled={isUploading}
      />
    </Box>
  );
}
