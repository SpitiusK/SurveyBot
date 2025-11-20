import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import {
  Box,
  Button,
  LinearProgress,
  Typography,
  Paper,
  Alert,
  IconButton,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  Close as CloseIcon,
  CheckCircle as SuccessIcon,
} from '@mui/icons-material';
import { MediaPreview } from './MediaPreview';
import type {
  MediaItemDto,
  MediaType,
  UploadProgress,
} from '../types/media';
import {
  MediaFileSizeLimits,
  AcceptedMimeTypes,
} from '../types/media';
import { getApiBaseUrl } from '@/config/ngrok.config';

interface MediaPickerProps {
  onMediaSelected: (media: MediaItemDto) => void;
  onError?: (error: string) => void;
  mediaType: MediaType;
  disabled?: boolean;
}

export const MediaPicker: React.FC<MediaPickerProps> = ({
  onMediaSelected,
  onError,
  mediaType,
  disabled = false,
}) => {
  const [uploadProgress, setUploadProgress] = useState<UploadProgress>({
    isUploading: false,
    progress: 0,
  });
  const [previewMedia, setPreviewMedia] = useState<MediaItemDto | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);

  const validateFile = (file: File): string | null => {
    const maxSize = MediaFileSizeLimits[mediaType];
    const acceptedTypes = Object.keys(AcceptedMimeTypes[mediaType]);

    if (file.size > maxSize) {
      return `File size exceeds the limit of ${(maxSize / 1024 / 1024).toFixed(0)} MB`;
    }

    if (!acceptedTypes.includes(file.type)) {
      const extensions = Object.values(AcceptedMimeTypes[mediaType]).flat().join(', ');
      return `File type not supported. Accepted types: ${extensions}`;
    }

    return null;
  };

  const uploadMedia = async (
    file: File,
    onProgress: (progress: number) => void
  ): Promise<MediaItemDto> => {
    const formData = new FormData();
    formData.append('file', file);

    const xhr = new XMLHttpRequest();

    // Track upload progress
    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable) {
        const percentComplete = Math.round((event.loaded / event.total) * 100);
        onProgress(percentComplete);
      }
    });

    return new Promise((resolve, reject) => {
      xhr.addEventListener('load', () => {
        if (xhr.status === 201) {
          try {
            const response = JSON.parse(xhr.responseText);
            resolve(response.data);
          } catch (error) {
            reject(new Error('Invalid server response'));
          }
        } else if (xhr.status === 400) {
          try {
            const error = JSON.parse(xhr.responseText);
            reject(new Error(error.message || 'Validation failed'));
          } catch {
            reject(new Error('Validation failed'));
          }
        } else if (xhr.status === 413) {
          reject(new Error('File size exceeds server limit'));
        } else if (xhr.status === 401) {
          reject(new Error('Authentication required. Please log in.'));
        } else {
          reject(new Error(`Upload failed with status ${xhr.status}`));
        }
      });

      xhr.addEventListener('error', () => {
        reject(new Error('Network error occurred during upload'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Upload cancelled'));
      });

      // Get API base URL using the same resolution logic as axios client
      // This ensures consistency with ngrok configuration and environment variables
      const apiBaseUrl = getApiBaseUrl();
      xhr.open('POST', `${apiBaseUrl}/media/upload?mediaType=${mediaType}`);

      // Get JWT token from localStorage
      const token = localStorage.getItem('authToken');
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }

      // Add ngrok bypass header
      xhr.setRequestHeader('ngrok-skip-browser-warning', 'true');

      xhr.send(formData);
    });
  };

  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      if (acceptedFiles.length === 0) return;
      if (disabled) return;

      const file = acceptedFiles[0];

      try {
        // Client-side validation
        const validationError = validateFile(file);
        if (validationError) {
          onError?.(validationError);
          setUploadProgress({
            isUploading: false,
            progress: 0,
            error: validationError,
          });
          return;
        }

        // Start upload
        setUploadProgress({
          isUploading: true,
          progress: 0,
          fileName: file.name,
        });
        setPreviewMedia(null);
        setShowSuccess(false);

        const media = await uploadMedia(file, (progress) => {
          setUploadProgress({
            isUploading: true,
            progress,
            fileName: file.name,
          });
        });

        // Show preview
        setPreviewMedia(media);
        setUploadProgress({ isUploading: false, progress: 100 });
        setShowSuccess(true);

        // Notify parent after short delay
        setTimeout(() => {
          onMediaSelected(media);
          setPreviewMedia(null);
          setShowSuccess(false);
        }, 1500);
      } catch (error) {
        const errorMessage =
          error instanceof Error ? error.message : 'Upload failed';
        onError?.(errorMessage);
        setUploadProgress({
          isUploading: false,
          progress: 0,
          error: errorMessage,
        });
      }
    },
    [mediaType, onMediaSelected, onError, disabled]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    accept: AcceptedMimeTypes[mediaType],
    disabled,
    noClick: uploadProgress.isUploading,
  });

  const clearError = () => {
    setUploadProgress({
      isUploading: false,
      progress: 0,
    });
  };

  const getMediaTypeLabel = () => {
    const labels: Record<MediaType, string> = {
      image: 'image',
      video: 'video',
      audio: 'audio',
      document: 'document',
    };
    return labels[mediaType];
  };

  const getAcceptedExtensions = () => {
    return Object.values(AcceptedMimeTypes[mediaType])
      .flat()
      .join(', ');
  };

  return (
    <Box sx={{ width: '100%' }}>
      <Paper
        {...getRootProps()}
        elevation={0}
        sx={{
          p: 3,
          border: '2px dashed',
          borderColor: isDragActive
            ? 'primary.main'
            : uploadProgress.error
            ? 'error.main'
            : 'divider',
          borderRadius: 2,
          backgroundColor: isDragActive
            ? 'action.hover'
            : disabled
            ? 'action.disabledBackground'
            : 'background.paper',
          cursor: disabled || uploadProgress.isUploading ? 'default' : 'pointer',
          transition: 'all 0.2s ease-in-out',
          '&:hover': {
            borderColor: disabled || uploadProgress.isUploading ? 'divider' : 'primary.main',
            backgroundColor: disabled || uploadProgress.isUploading ? undefined : 'action.hover',
          },
        }}
        role="button"
        tabIndex={disabled || uploadProgress.isUploading ? -1 : 0}
        aria-label={`Drop ${getMediaTypeLabel()} files here or click to browse`}
        aria-disabled={disabled || uploadProgress.isUploading}
      >
        <input {...getInputProps()} aria-label="File input" />

        {uploadProgress.isUploading ? (
          <Box sx={{ textAlign: 'center' }}>
            <UploadIcon sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />
            <Typography variant="body1" gutterBottom>
              Uploading {uploadProgress.fileName}...
            </Typography>
            <LinearProgress
              variant="determinate"
              value={uploadProgress.progress}
              sx={{ mt: 2, mb: 1, height: 8, borderRadius: 4 }}
            />
            <Typography variant="body2" color="text.secondary">
              {uploadProgress.progress}%
            </Typography>
          </Box>
        ) : showSuccess && previewMedia ? (
          <Box sx={{ textAlign: 'center' }}>
            <SuccessIcon sx={{ fontSize: 48, color: 'success.main', mb: 2 }} />
            <Typography variant="body1" color="success.main" gutterBottom>
              Upload successful!
            </Typography>
            <MediaPreview media={previewMedia} mediaType={mediaType} />
          </Box>
        ) : (
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              textAlign: 'center',
            }}
          >
            <UploadIcon
              sx={{
                fontSize: 48,
                color: disabled ? 'action.disabled' : 'primary.main',
                mb: 2,
              }}
            />
            <Typography variant="h6" gutterBottom>
              {isDragActive
                ? `Drop your ${getMediaTypeLabel()} file here`
                : `Drag and drop your ${getMediaTypeLabel()} file here`}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              or
            </Typography>
            <Button
              variant="contained"
              disabled={disabled}
              type="button"
              startIcon={<UploadIcon />}
            >
              Click to browse
            </Button>
            <Typography variant="caption" color="text.secondary" sx={{ mt: 2 }}>
              Accepted formats: {getAcceptedExtensions()}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Max size: {(MediaFileSizeLimits[mediaType] / 1024 / 1024).toFixed(0)} MB
            </Typography>
          </Box>
        )}
      </Paper>

      {uploadProgress.error && (
        <Alert
          severity="error"
          sx={{ mt: 2 }}
          action={
            <IconButton
              aria-label="close"
              color="inherit"
              size="small"
              onClick={clearError}
            >
              <CloseIcon fontSize="inherit" />
            </IconButton>
          }
        >
          {uploadProgress.error}
        </Alert>
      )}
    </Box>
  );
};
