import React, { useState, useCallback } from 'react';
import { MediaGalleryItem } from './MediaGalleryItem';
import { MediaPicker } from './MediaPicker';
import type { MediaItemDto, MediaType } from '../types/media';
import {
  Grid,
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Typography,
  Paper,
} from '@mui/material';
import {
  AddPhotoAlternate as AddPhotoAlternateIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import './MediaGallery.css';

interface MediaGalleryProps {
  mediaItems: MediaItemDto[];
  onAddMedia?: (media: MediaItemDto) => void;
  onRemoveMedia?: (mediaId: string) => void;
  onReorderMedia?: (items: MediaItemDto[]) => void;
  readOnly?: boolean;
  mediaType?: MediaType;
  acceptedTypes?: MediaType[];
}

export const MediaGallery: React.FC<MediaGalleryProps> = ({
  mediaItems = [],
  onAddMedia,
  onRemoveMedia,
  onReorderMedia,
  readOnly = false,
  mediaType = 'image',
  acceptedTypes,
}) => {
  const [mediaDialogOpen, setMediaDialogOpen] = useState(false);
  const [currentMediaType, setCurrentMediaType] = useState<MediaType>(mediaType);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [mediaToDelete, setMediaToDelete] = useState<string | null>(null);
  const [draggedItem, setDraggedItem] = useState<MediaItemDto | null>(null);
  const [dropTargetId, setDropTargetId] = useState<string | null>(null);

  // If acceptedTypes are provided, cycle through them
  const availableTypes = acceptedTypes && acceptedTypes.length > 0 ? acceptedTypes : [mediaType];

  const handleAddMedia = useCallback(
    (media: MediaItemDto) => {
      onAddMedia?.(media);
      setMediaDialogOpen(false);
    },
    [onAddMedia]
  );

  const handleDeleteClick = useCallback((mediaId: string) => {
    setMediaToDelete(mediaId);
    setDeleteConfirmOpen(true);
  }, []);

  const handleConfirmDelete = useCallback(() => {
    if (mediaToDelete) {
      onRemoveMedia?.(mediaToDelete);
    }
    setDeleteConfirmOpen(false);
    setMediaToDelete(null);
  }, [mediaToDelete, onRemoveMedia]);

  const handleCancelDelete = useCallback(() => {
    setDeleteConfirmOpen(false);
    setMediaToDelete(null);
  }, []);

  const handleDragStart = useCallback((media: MediaItemDto) => {
    setDraggedItem(media);
  }, []);

  const handleDragEnd = useCallback(() => {
    setDraggedItem(null);
    setDropTargetId(null);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent, targetId: string) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDropTargetId(targetId);
  }, []);

  const handleDragLeave = useCallback(() => {
    setDropTargetId(null);
  }, []);

  const handleDrop = useCallback(
    (targetMedia: MediaItemDto) => {
      if (!draggedItem || !onReorderMedia) return;

      const items = [...mediaItems];
      const dragIndex = items.findIndex((m) => m.id === draggedItem.id);
      const targetIndex = items.findIndex((m) => m.id === targetMedia.id);

      if (dragIndex !== -1 && targetIndex !== -1 && dragIndex !== targetIndex) {
        // Swap items
        [items[dragIndex], items[targetIndex]] = [items[targetIndex], items[dragIndex]];

        // Update order property for all items
        items.forEach((m, i) => (m.order = i));

        onReorderMedia(items);
      }

      setDraggedItem(null);
      setDropTargetId(null);
    },
    [draggedItem, mediaItems, onReorderMedia]
  );

  const getMediaTypeName = (type: MediaType = currentMediaType) => {
    const names: Record<MediaType, string> = {
      image: 'images',
      video: 'videos',
      audio: 'audio files',
      document: 'documents',
      archive: 'archives',
    };
    return names[type];
  };

  const cycleMediaType = () => {
    const currentIndex = availableTypes.indexOf(currentMediaType);
    const nextIndex = (currentIndex + 1) % availableTypes.length;
    setCurrentMediaType(availableTypes[nextIndex]);
  };

  return (
    <Box className="media-gallery-container">
      {/* Header */}
      <Box className="gallery-header" sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h6" component="h3">
          Media Gallery
        </Typography>
        {!readOnly && (
          <Box sx={{ display: 'flex', gap: 1 }}>
            {availableTypes.length > 1 && (
              <Button
                variant="text"
                size="small"
                onClick={cycleMediaType}
                sx={{ textTransform: 'capitalize' }}
              >
                Filter: {getMediaTypeName()}
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<AddPhotoAlternateIcon />}
              onClick={() => setMediaDialogOpen(true)}
              size="small"
            >
              Add Media
            </Button>
          </Box>
        )}
      </Box>

      {/* Empty State */}
      {mediaItems.length === 0 && (
        <Paper
          elevation={0}
          sx={{
            p: 4,
            textAlign: 'center',
            border: '1px dashed',
            borderColor: 'divider',
            borderRadius: 2,
          }}
        >
          <AddPhotoAlternateIcon
            sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }}
          />
          <Typography variant="body1" color="text.secondary" gutterBottom>
            No {getMediaTypeName()} attached yet
          </Typography>
          {!readOnly && (
            <Button
              variant="contained"
              startIcon={<AddPhotoAlternateIcon />}
              onClick={() => setMediaDialogOpen(true)}
              sx={{ mt: 2 }}
            >
              Add Your First Media
            </Button>
          )}
        </Paper>
      )}

      {/* Grid Layout */}
      {mediaItems.length > 0 && (
        <Grid container spacing={2} className="gallery-grid">
          {mediaItems.map((media) => (
            <Grid item xs={12} sm={6} md={4} key={media.id}>
              <MediaGalleryItem
                media={media}
                onDelete={() => handleDeleteClick(media.id)}
                onDragStart={() => handleDragStart(media)}
                onDragEnd={handleDragEnd}
                onDragOver={(e) => handleDragOver(e, media.id)}
                onDragLeave={handleDragLeave}
                onDrop={() => handleDrop(media)}
                readOnly={readOnly}
                isDragging={draggedItem?.id === media.id}
                isDropTarget={dropTargetId === media.id}
              />
            </Grid>
          ))}
        </Grid>
      )}

      {/* Add Media Dialog */}
      <Dialog
        open={mediaDialogOpen}
        onClose={() => setMediaDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="h6">Add Media</Typography>
            <IconButton
              onClick={() => setMediaDialogOpen(false)}
              size="small"
              aria-label="Close dialog"
            >
              <CloseIcon />
            </IconButton>
          </Box>
        </DialogTitle>
        <DialogContent>
          <MediaPicker
            mediaType={currentMediaType}
            onMediaSelected={handleAddMedia}
          />
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteConfirmOpen}
        onClose={handleCancelDelete}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <Typography variant="body1">
            Are you sure you want to delete this media?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCancelDelete}>Cancel</Button>
          <Button
            onClick={handleConfirmDelete}
            variant="contained"
            color="error"
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
