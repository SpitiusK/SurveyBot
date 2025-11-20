import React from 'react';
import {
  Card,
  CardMedia,
  CardContent,
  IconButton,
  Tooltip,
  Box,
  Typography,
  Chip,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  DragIndicator as DragIcon,
  Image as ImageIcon,
  VideoLibrary as VideoIcon,
  AudioFile as AudioIcon,
  Description as DocumentIcon,
} from '@mui/icons-material';
import type { MediaItemDto } from '../types/media';

interface MediaGalleryItemProps {
  media: MediaItemDto;
  onDelete: () => void;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDragOver: (e: React.DragEvent) => void;
  onDragLeave: () => void;
  onDrop: () => void;
  readOnly?: boolean;
  isDragging?: boolean;
  isDropTarget?: boolean;
}

export const MediaGalleryItem: React.FC<MediaGalleryItemProps> = ({
  media,
  onDelete,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDragLeave,
  onDrop,
  readOnly = false,
  isDragging = false,
  isDropTarget = false,
}) => {
  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
  };

  const getMediaIcon = () => {
    const iconProps = { sx: { fontSize: 64, color: 'primary.main' } };
    switch (media.type) {
      case 'image':
        return <ImageIcon {...iconProps} />;
      case 'video':
        return <VideoIcon {...iconProps} />;
      case 'audio':
        return <AudioIcon {...iconProps} />;
      case 'document':
        return <DocumentIcon {...iconProps} />;
      default:
        return <ImageIcon {...iconProps} />;
    }
  };

  const getMediaTypeColor = () => {
    const colors = {
      image: 'primary',
      video: 'secondary',
      audio: 'info',
      document: 'warning',
    } as const;
    return colors[media.type] || 'default';
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    onDrop();
  };

  return (
    <Card
      className={`media-gallery-item ${isDragging ? 'dragging' : ''} ${isDropTarget ? 'drop-target' : ''}`}
      draggable={!readOnly}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={handleDrop}
      sx={{
        position: 'relative',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        opacity: isDragging ? 0.5 : 1,
        transition: 'all 0.2s ease-in-out',
        cursor: !readOnly ? 'grab' : 'default',
        border: isDropTarget ? '2px solid' : '1px solid',
        borderColor: isDropTarget ? 'primary.main' : 'divider',
        '&:hover': {
          boxShadow: 3,
          transform: isDragging ? 'none' : 'translateY(-4px)',
        },
        '&:active': {
          cursor: !readOnly ? 'grabbing' : 'default',
        },
      }}
    >
      {/* Drag Handle */}
      {!readOnly && (
        <Box
          sx={{
            position: 'absolute',
            top: 8,
            left: 8,
            zIndex: 2,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            borderRadius: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'grab',
            '&:active': {
              cursor: 'grabbing',
            },
          }}
        >
          <DragIcon sx={{ color: 'white', fontSize: 20 }} />
        </Box>
      )}

      {/* Delete Button */}
      {!readOnly && (
        <Box
          sx={{
            position: 'absolute',
            top: 8,
            right: 8,
            zIndex: 2,
          }}
        >
          <Tooltip title="Delete media">
            <IconButton
              size="small"
              onClick={onDelete}
              className="delete-button"
              sx={{
                backgroundColor: 'rgba(0, 0, 0, 0.5)',
                color: 'white',
                '&:hover': {
                  backgroundColor: 'error.main',
                },
              }}
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      )}

      {/* Media Preview */}
      {media.type === 'image' && media.thumbnailPath ? (
        <CardMedia
          component="img"
          height="200"
          image={media.thumbnailPath}
          alt={media.altText || media.displayName}
          sx={{
            objectFit: 'cover',
            backgroundColor: 'grey.100',
          }}
        />
      ) : (
        <Box
          sx={{
            height: 200,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: 'grey.100',
          }}
        >
          {getMediaIcon()}
        </Box>
      )}

      {/* Content */}
      <CardContent sx={{ flexGrow: 1, pt: 2 }}>
        <Box sx={{ mb: 1 }}>
          <Chip
            label={media.type}
            color={getMediaTypeColor()}
            size="small"
            sx={{ mb: 1 }}
          />
        </Box>

        <Tooltip title={media.displayName}>
          <Typography
            variant="body2"
            component="p"
            sx={{
              fontWeight: 500,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              mb: 0.5,
            }}
          >
            {media.displayName}
          </Typography>
        </Tooltip>

        <Typography variant="caption" color="text.secondary" display="block">
          {formatFileSize(media.fileSize)}
        </Typography>

        {media.altText && (
          <Typography
            variant="caption"
            color="text.secondary"
            display="block"
            sx={{
              mt: 1,
              fontStyle: 'italic',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            Alt: {media.altText}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
};
