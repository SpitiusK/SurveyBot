import React from 'react';
import { Box, Typography, Paper } from '@mui/material';
import {
  Image as ImageIcon,
  Videocam as VideoIcon,
  AudioFile as AudioIcon,
  Description as DocumentIcon,
} from '@mui/icons-material';
import type { MediaItemDto, MediaType } from '../types/media';

interface MediaPreviewProps {
  media: MediaItemDto;
  mediaType: MediaType;
}

export const MediaPreview: React.FC<MediaPreviewProps> = ({ media, mediaType }) => {
  const renderIcon = () => {
    switch (mediaType) {
      case 'image':
        return <ImageIcon sx={{ fontSize: 48, color: 'primary.main' }} />;
      case 'video':
        return <VideoIcon sx={{ fontSize: 48, color: 'primary.main' }} />;
      case 'audio':
        return <AudioIcon sx={{ fontSize: 48, color: 'primary.main' }} />;
      case 'document':
        return <DocumentIcon sx={{ fontSize: 48, color: 'primary.main' }} />;
      default:
        return <DocumentIcon sx={{ fontSize: 48, color: 'primary.main' }} />;
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  };

  return (
    <Paper
      elevation={2}
      sx={{
        p: 2,
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        backgroundColor: 'background.default',
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: 80,
          height: 80,
          borderRadius: 1,
          backgroundColor: 'background.paper',
        }}
      >
        {mediaType === 'image' && media.thumbnailPath ? (
          <img
            src={media.thumbnailPath}
            alt={media.displayName}
            style={{
              maxWidth: '100%',
              maxHeight: '100%',
              objectFit: 'cover',
              borderRadius: '4px',
            }}
          />
        ) : (
          renderIcon()
        )}
      </Box>

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography
          variant="body1"
          sx={{
            fontWeight: 500,
            mb: 0.5,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {media.displayName}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {formatFileSize(media.fileSize)} • {media.mimeType}
        </Typography>
      </Box>
    </Paper>
  );
};
