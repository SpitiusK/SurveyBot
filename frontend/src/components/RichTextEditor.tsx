import React, { useState, useCallback, useMemo } from 'react';
import ReactQuill, { Quill } from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import { UnifiedMediaPicker } from './UnifiedMediaPicker';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Grid,
  Card,
  CardMedia,
  CardContent,
  IconButton,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  Close as CloseIcon,
  Delete as DeleteIcon,
  Image as ImageIcon,
  Videocam as VideoIcon,
  AudioFile as AudioIcon,
  Description as DocumentIcon,
  Archive as ArchiveIcon,
} from '@mui/icons-material';
import type { MediaContentDto, MediaItemDto, MediaType } from '../types/media';

interface RichTextEditorProps {
  value: string;
  onChange: (content: string, mediaContent?: MediaContentDto) => void;
  onError?: (error: string) => void;
  placeholder?: string;
  readOnly?: boolean;
  initialMedia?: MediaItemDto[];
  mediaType?: MediaType;
  acceptedTypes?: MediaType[];
}

export const RichTextEditor: React.FC<RichTextEditorProps> = ({
  value,
  onChange,
  onError,
  placeholder = 'Enter question text...',
  readOnly = false,
  initialMedia = [],
  mediaType = 'image',
  acceptedTypes,
}) => {
  const [mediaDialogOpen, setMediaDialogOpen] = useState(false);
  const [currentMediaType, setCurrentMediaType] = useState<MediaType>(mediaType);
  const [mediaItems, setMediaItems] = useState<MediaItemDto[]>(initialMedia);
  const [editorContent, setEditorContent] = useState(value);

  // If acceptedTypes are provided, use them; otherwise fall back to mediaType
  const availableTypes = acceptedTypes && acceptedTypes.length > 0 ? acceptedTypes : [mediaType];

  const cycleMediaType = () => {
    const currentIndex = availableTypes.indexOf(currentMediaType);
    const nextIndex = (currentIndex + 1) % availableTypes.length;
    setCurrentMediaType(availableTypes[nextIndex]);
  };

  // Custom toolbar module with media button handler
  // Only includes Telegram-supported HTML formatting (bold, italic, underline, strikethrough, links)
  // Removed: blockquote, code-block, headers (h1-h6), lists (ordered, bullet)
  const modules = useMemo(
    () => ({
      toolbar: {
        container: [
          ['bold', 'italic', 'underline', 'strike'],
          ['link'],
          ...(readOnly ? [] : [['insertMedia']]),
          ['clean'],
        ],
        handlers: readOnly
          ? {}
          : {
              insertMedia: () => {
                setMediaDialogOpen(true);
              },
            },
      },
    }),
    [readOnly]
  );

  // Only include formats supported by Telegram HTML mode
  const formats = [
    'bold',
    'italic',
    'underline',
    'strike',
    'link',
  ];

  const handleEditorChange = useCallback(
    (content: string) => {
      setEditorContent(content);
      const mediaContent: MediaContentDto = {
        version: '1.0',
        items: mediaItems,
      };
      onChange(content, mediaContent);
    },
    [mediaItems, onChange]
  );

  const handleMediaSelected = useCallback(
    (media: MediaItemDto) => {
      const updatedMedia = [
        ...mediaItems,
        {
          ...media,
          order: mediaItems.length,
        },
      ];
      setMediaItems(updatedMedia);
      setMediaDialogOpen(false);

      const mediaContent: MediaContentDto = {
        version: '1.0',
        items: updatedMedia,
      };
      onChange(editorContent, mediaContent);
    },
    [mediaItems, editorContent, onChange]
  );

  const handleRemoveMedia = useCallback(
    (mediaId: string) => {
      const updatedMedia = mediaItems
        .filter((m) => m.id !== mediaId)
        .map((item, index) => ({ ...item, order: index }));
      setMediaItems(updatedMedia);

      const mediaContent: MediaContentDto = {
        version: '1.0',
        items: updatedMedia,
      };
      onChange(editorContent, mediaContent);
    },
    [mediaItems, editorContent, onChange]
  );

  const renderMediaIcon = (type: MediaType) => {
    switch (type) {
      case 'image':
        return <ImageIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
      case 'video':
        return <VideoIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
      case 'audio':
        return <AudioIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
      case 'document':
        return <DocumentIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
      case 'archive':
        return <ArchiveIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
      default:
        return <DocumentIcon sx={{ fontSize: 48, color: 'text.secondary' }} />;
    }
  };

  return (
    <Box className="rich-text-editor-container">
      {/* Editor */}
      <Box
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          backgroundColor: 'background.paper',
          '& .ql-toolbar': {
            borderTopLeftRadius: 4,
            borderTopRightRadius: 4,
            backgroundColor: 'grey.50',
            borderBottom: '1px solid',
            borderColor: 'divider',
          },
          '& .ql-container': {
            borderBottomLeftRadius: 4,
            borderBottomRightRadius: 4,
            fontSize: '16px',
            minHeight: '300px',
          },
          '& .ql-editor': {
            minHeight: '300px',
          },
          '& .ql-editor.ql-blank::before': {
            color: 'text.disabled',
            fontStyle: 'italic',
          },
        }}
      >
        <ReactQuill
          theme="snow"
          value={editorContent}
          onChange={handleEditorChange}
          modules={modules}
          formats={formats}
          placeholder={placeholder}
          readOnly={readOnly}
        />
      </Box>

      {/* Media Gallery */}
      {mediaItems.length > 0 && (
        <Box
          sx={{
            mt: 3,
            p: 2,
            backgroundColor: 'grey.50',
            borderRadius: 1,
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          <Typography variant="h6" sx={{ mb: 2 }}>
            Attached Media ({mediaItems.length})
          </Typography>
          <Grid container spacing={2}>
            {mediaItems.map((media) => (
              <Grid item xs={12} sm={6} md={4} key={media.id}>
                <Card
                  sx={{
                    position: 'relative',
                    height: '100%',
                    '&:hover .delete-button': {
                      opacity: 1,
                    },
                  }}
                >
                  {/* Media Preview */}
                  {media.type === 'image' && media.thumbnailPath ? (
                    <CardMedia
                      component="img"
                      height="140"
                      image={media.thumbnailPath}
                      alt={media.displayName}
                      sx={{ objectFit: 'cover' }}
                    />
                  ) : (
                    <Box
                      sx={{
                        height: 140,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        backgroundColor: 'grey.100',
                      }}
                    >
                      {renderMediaIcon(media.type)}
                    </Box>
                  )}

                  {/* Delete Button (shows on hover) */}
                  {!readOnly && (
                    <Tooltip title="Delete media">
                      <IconButton
                        className="delete-button"
                        size="small"
                        onClick={() => handleRemoveMedia(media.id)}
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          backgroundColor: 'rgba(255, 255, 255, 0.95)',
                          opacity: 0,
                          transition: 'opacity 0.2s',
                          '&:hover': {
                            backgroundColor: 'rgba(255, 255, 255, 1)',
                          },
                        }}
                      >
                        <DeleteIcon fontSize="small" color="error" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {/* Media Info */}
                  <CardContent>
                    <Typography
                      variant="body2"
                      sx={{
                        fontWeight: 500,
                        whiteSpace: 'nowrap',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        mb: 0.5,
                      }}
                      title={media.displayName}
                    >
                      {media.displayName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {(media.fileSize / 1024 / 1024).toFixed(2)} MB
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Box>
      )}

      {/* Media Picker Modal */}
      <Dialog
        open={mediaDialogOpen}
        onClose={() => setMediaDialogOpen(false)}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 2,
          },
        }}
      >
        <DialogTitle
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            pb: 1,
          }}
        >
          <Typography variant="h6">Upload Media</Typography>
          <IconButton
            onClick={() => setMediaDialogOpen(false)}
            size="small"
            aria-label="Close dialog"
          >
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent sx={{ pt: 2 }}>
          <Box sx={{ mb: 2 }}>
            {availableTypes.length > 1 && (
              <Button
                variant="text"
                size="small"
                onClick={cycleMediaType}
                sx={{ textTransform: 'capitalize', mb: 1 }}
              >
                Current type: {currentMediaType}
              </Button>
            )}
          </Box>
          <UnifiedMediaPicker
            mediaType={currentMediaType}
            onMediaSelected={handleMediaSelected}
            onError={(error) => {
              onError?.(error);
            }}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setMediaDialogOpen(false)} color="inherit">
            Cancel
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

// Custom toolbar button for media insertion
// This needs to be registered before using the component
if (typeof window !== 'undefined') {
  const icons = Quill.import('ui/icons');
  icons['insertMedia'] = '<svg viewBox="0 0 18 18"><rect class="ql-stroke" height="10" width="12" x="3" y="4"></rect><circle class="ql-fill" cx="6" cy="7" r="1"></circle><polyline class="ql-even ql-fill" points="5 12 5 11 7 9 8 10 11 7 13 9 13 12 5 12"></polyline></svg>';
}
