import React, { useState } from 'react';
import { MediaGallery } from './MediaGallery';
import type { MediaItemDto, MediaType } from '../types/media';
import { Box, Container, Typography, Paper } from '@mui/material';

/**
 * Example usage of MediaGallery component
 *
 * This demonstrates how to integrate MediaGallery into your forms
 */

export const MediaGalleryExample: React.FC = () => {
  const [imageMedia, setImageMedia] = useState<MediaItemDto[]>([
    {
      id: '1',
      type: 'image',
      filePath: '/uploads/image1.jpg',
      displayName: 'Mountain Landscape.jpg',
      fileSize: 2450000,
      mimeType: 'image/jpeg',
      uploadedAt: new Date().toISOString(),
      altText: 'Beautiful mountain landscape at sunset',
      thumbnailPath: '/uploads/thumbnails/image1.jpg',
      order: 0,
    },
    {
      id: '2',
      type: 'image',
      filePath: '/uploads/image2.png',
      displayName: 'City Skyline.png',
      fileSize: 1820000,
      mimeType: 'image/png',
      uploadedAt: new Date().toISOString(),
      thumbnailPath: '/uploads/thumbnails/image2.png',
      order: 1,
    },
  ]);

  const [videoMedia, setVideoMedia] = useState<MediaItemDto[]>([
    {
      id: '3',
      type: 'video',
      filePath: '/uploads/video1.mp4',
      displayName: 'Product Demo.mp4',
      fileSize: 15600000,
      mimeType: 'video/mp4',
      uploadedAt: new Date().toISOString(),
      order: 0,
    },
  ]);

  const [audioMedia, setAudioMedia] = useState<MediaItemDto[]>([]);
  const [documentMedia, setDocumentMedia] = useState<MediaItemDto[]>([]);

  // Handler for adding media
  const handleAddMedia = (
    items: MediaItemDto[],
    setItems: React.Dispatch<React.SetStateAction<MediaItemDto[]>>
  ) => {
    return (media: MediaItemDto) => {
      const newMedia = {
        ...media,
        order: items.length,
      };
      setItems([...items, newMedia]);
    };
  };

  // Handler for removing media
  const handleRemoveMedia = (
    items: MediaItemDto[],
    setItems: React.Dispatch<React.SetStateAction<MediaItemDto[]>>
  ) => {
    return (mediaId: string) => {
      const filtered = items.filter((m) => m.id !== mediaId);
      // Update order
      filtered.forEach((m, i) => (m.order = i));
      setItems(filtered);
    };
  };

  // Handler for reordering media
  const handleReorderMedia = (
    setItems: React.Dispatch<React.SetStateAction<MediaItemDto[]>>
  ) => {
    return (reorderedItems: MediaItemDto[]) => {
      setItems(reorderedItems);
    };
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" gutterBottom>
        MediaGallery Component Examples
      </Typography>

      <Typography variant="body1" color="text.secondary" paragraph>
        The MediaGallery component provides a comprehensive interface for managing
        media attachments with drag-and-drop reordering, add/remove functionality,
        and responsive grid layout.
      </Typography>

      {/* Image Gallery */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h5" gutterBottom>
          Image Gallery (Editable)
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          This gallery allows adding images, removing them, and reordering via drag-and-drop.
        </Typography>
        <MediaGallery
          mediaItems={imageMedia}
          onAddMedia={handleAddMedia(imageMedia, setImageMedia)}
          onRemoveMedia={handleRemoveMedia(imageMedia, setImageMedia)}
          onReorderMedia={handleReorderMedia(setImageMedia)}
          mediaType="image"
        />
      </Paper>

      {/* Video Gallery */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h5" gutterBottom>
          Video Gallery (Editable)
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          Supports video uploads with type-specific icons for non-image media.
        </Typography>
        <MediaGallery
          mediaItems={videoMedia}
          onAddMedia={handleAddMedia(videoMedia, setVideoMedia)}
          onRemoveMedia={handleRemoveMedia(videoMedia, setVideoMedia)}
          onReorderMedia={handleReorderMedia(setVideoMedia)}
          mediaType="video"
        />
      </Paper>

      {/* Audio Gallery */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h5" gutterBottom>
          Audio Gallery (Empty State)
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          Empty state with clear call-to-action for adding media.
        </Typography>
        <MediaGallery
          mediaItems={audioMedia}
          onAddMedia={handleAddMedia(audioMedia, setAudioMedia)}
          onRemoveMedia={handleRemoveMedia(audioMedia, setAudioMedia)}
          onReorderMedia={handleReorderMedia(setAudioMedia)}
          mediaType="audio"
        />
      </Paper>

      {/* Document Gallery */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h5" gutterBottom>
          Document Gallery (Empty State)
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          Supports document uploads (PDF, DOC, DOCX, TXT).
        </Typography>
        <MediaGallery
          mediaItems={documentMedia}
          onAddMedia={handleAddMedia(documentMedia, setDocumentMedia)}
          onRemoveMedia={handleRemoveMedia(documentMedia, setDocumentMedia)}
          onReorderMedia={handleReorderMedia(setDocumentMedia)}
          mediaType="document"
        />
      </Paper>

      {/* Read-Only Gallery */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h5" gutterBottom>
          Read-Only Gallery
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          Display-only mode with no add, delete, or reorder capabilities.
        </Typography>
        <MediaGallery
          mediaItems={imageMedia}
          readOnly={true}
          mediaType="image"
        />
      </Paper>

      {/* Integration Example */}
      <Paper sx={{ p: 3, backgroundColor: 'grey.50' }}>
        <Typography variant="h6" gutterBottom>
          Integration in QuestionForm
        </Typography>
        <Typography variant="body2" component="pre" sx={{
          backgroundColor: 'grey.900',
          color: 'grey.100',
          p: 2,
          borderRadius: 1,
          overflow: 'auto',
        }}>
{`// In your QuestionForm component:
import { MediaGallery } from './MediaGallery';

const QuestionForm = () => {
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>([]);

  const handleAddMedia = (media: MediaItemDto) => {
    setMediaItems([...mediaItems, { ...media, order: mediaItems.length }]);
  };

  const handleRemoveMedia = (mediaId: string) => {
    const filtered = mediaItems.filter(m => m.id !== mediaId);
    filtered.forEach((m, i) => m.order = i);
    setMediaItems(filtered);
  };

  const handleReorderMedia = (reordered: MediaItemDto[]) => {
    setMediaItems(reordered);
  };

  return (
    <form>
      {/* Other form fields */}

      <MediaGallery
        mediaItems={mediaItems}
        onAddMedia={handleAddMedia}
        onRemoveMedia={handleRemoveMedia}
        onReorderMedia={handleReorderMedia}
        mediaType="image"
      />

      {/* Submit button */}
    </form>
  );
};`}
        </Typography>
      </Paper>
    </Container>
  );
};

export default MediaGalleryExample;
