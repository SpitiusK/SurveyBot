/**
 * Unified Media Picker - Accept any file type with auto-detection
 * Single button for drag-and-drop file upload
 * Replaces type-specific MediaPicker for seamless user experience
 */

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
import type { MediaItemDto, UploadProgress, MediaType } from '../types/media';
import {
  MAX_FILE_SIZE,
} from '../types/media';
import { detectFileType, getMediaTypeLabel } from '@/utils/fileTypeDetector';
import { getApiBaseUrl } from '@/config/ngrok.config';

interface UnifiedMediaPickerProps {
  onMediaSelected: (media: MediaItemDto) => void;
  onError?: (error: string) => void;
  disabled?: boolean;
  mediaType?: MediaType;
}

export const UnifiedMediaPicker: React.FC<UnifiedMediaPickerProps> = ({
  onMediaSelected,
  onError,
  disabled = false,
  mediaType,
}) => {
  const [uploadProgress, setUploadProgress] = useState<UploadProgress>({
    isUploading: false,
    progress: 0,
  });
  const [previewMedia, setPreviewMedia] = useState<MediaItemDto | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);
  const [detectedMediaType, setDetectedMediaType] = useState<string | null>(
    null
  );

  /**
   * Validates file before upload (client-side)
   */
  const validateFile = async (file: File): Promise<string | null> => {
    console.log(`[UnifiedMediaPicker] Validating file: ${file.name}`);

    // Check file size
    if (file.size > MAX_FILE_SIZE) {
      const errorMsg = `File size exceeds the limit of ${(MAX_FILE_SIZE / 1024 / 1024).toFixed(0)} MB`;
      console.warn(`[UnifiedMediaPicker] Size check failed: ${errorMsg}`);
      return errorMsg;
    }

    // Empty file check
    if (file.size === 0) {
      console.warn(`[UnifiedMediaPicker] File is empty`);
      return 'File is empty';
    }

    // Detect file type to ensure it's supported
    console.log(`[UnifiedMediaPicker] Starting file type detection...`);
    const { mediaType: detectedType, error } = await detectFileType(file);

    if (error) {
      console.error(`[UnifiedMediaPicker] Detection error: ${error}`);
      return error;
    }

    if (!detectedType) {
      const errorMsg = `Unsupported file type: ${file.name}`;
      console.error(`[UnifiedMediaPicker] No media type detected: ${errorMsg}`);
      return errorMsg;
    }

    // If a specific mediaType is requested, check if the detected type matches
    if (mediaType && detectedType !== mediaType) {
      const errorMsg = `Expected ${mediaType} file, but detected ${detectedType}. File: ${file.name}`;
      console.error(`[UnifiedMediaPicker] Type mismatch: ${errorMsg}`);
      return errorMsg;
    }

    console.log(`[UnifiedMediaPicker] Validation passed: ${file.name} (${detectedType})`);
    setDetectedMediaType(detectedType);
    return null;
  };

  /**
   * Uploads file with auto-detected type (no mediaType query parameter)
   */
  const uploadMedia = async (
    file: File,
    onProgress: (progress: number) => void
  ): Promise<MediaItemDto> => {
    const formData = new FormData();
    formData.append('file', file);
    // Note: mediaType NOT appended - API will auto-detect

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

      const apiBaseUrl = getApiBaseUrl();
      // API endpoint without mediaType parameter - auto-detection enabled
      xhr.open('POST', `${apiBaseUrl}/media/upload`);

      const token = localStorage.getItem('authToken');
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }

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
        const validationError = await validateFile(file);
        if (validationError) {
          onError?.(validationError);
          setUploadProgress({
            isUploading: false,
            progress: 0,
            error: validationError,
          });
          return;
        }

        // Detect file type
        const { mediaType } = await detectFileType(file);
        if (mediaType) {
          setDetectedMediaType(mediaType);
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
          setDetectedMediaType(null);
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
    [onMediaSelected, onError, disabled]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    // Accept all supported file types with explicit MIME types
    // Note: File browser uses this for filtering in file picker dialog
    accept: {
      // Images - explicit MIME types for proper file picker filtering
      'image/jpeg': ['.jpg', '.jpeg'],
      'image/png': ['.png'],
      'image/gif': ['.gif'],
      'image/webp': ['.webp'],
      'image/bmp': ['.bmp'],
      'image/tiff': ['.tiff', '.tif'],
      'image/x-icon': ['.ico'],
      'image/svg+xml': ['.svg'],
      // Videos - explicit MIME types instead of wildcard
      'video/mp4': ['.mp4'],
      'video/webm': ['.webm'],
      'video/quicktime': ['.mov'],
      'video/x-msvideo': ['.avi'],
      'video/x-matroska': ['.mkv'],
      'video/x-flv': ['.flv'],
      'video/x-ms-wmv': ['.wmv'],
      'video/3gpp': ['.3gp'],
      // Audio - explicit MIME types instead of wildcard
      'audio/mpeg': ['.mp3'],
      'audio/wav': ['.wav'],
      'audio/ogg': ['.ogg', '.oga'],
      'audio/mp4': ['.m4a'],
      'audio/flac': ['.flac'],
      'audio/aac': ['.aac'],
      'audio/x-ms-wma': ['.wma'],
      'audio/x-caf': ['.caf'],
      'audio/aiff': ['.aiff', '.aif'],
      // Documents
      'application/pdf': ['.pdf'],
      'application/msword': ['.doc'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
      'application/vnd.ms-excel': ['.xls'],
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
      'application/vnd.ms-powerpoint': ['.ppt'],
      'application/vnd.openxmlformats-officedocument.presentationml.presentation': ['.pptx'],
      'text/plain': ['.txt'],
      'text/rtf': ['.rtf'],
      'application/rtf': ['.rtf'],
      'application/vnd.oasis.opendocument.text': ['.odt'],
      'application/vnd.oasis.opendocument.spreadsheet': ['.ods'],
      'application/vnd.oasis.opendocument.presentation': ['.odp'],
      'text/csv': ['.csv'],
      'application/json': ['.json'],
      'application/xml': ['.xml'],
      'text/xml': ['.xml'],
      'text/markdown': ['.md'],
      'application/epub+zip': ['.epub'],
      // Archives
      'application/zip': ['.zip'],
      'application/x-rar-compressed': ['.rar'],
      'application/x-7z-compressed': ['.7z'],
      'application/x-tar': ['.tar'],
      'application/gzip': ['.gz'],
      'application/x-bzip2': ['.bz2'],
    },
    disabled,
    noClick: uploadProgress.isUploading,
  });

  const clearError = () => {
    setUploadProgress({
      isUploading: false,
      progress: 0,
    });
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
        aria-label="Drop files here or click to browse"
        aria-disabled={disabled || uploadProgress.isUploading}
      >
        <input {...getInputProps()} aria-label="File input" />

        {uploadProgress.isUploading ? (
          <Box sx={{ textAlign: 'center' }}>
            <UploadIcon sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />
            <Typography variant="body1" gutterBottom>
              Uploading {uploadProgress.fileName}...
              {detectedMediaType && (
                <Typography variant="caption" color="text.secondary" display="block">
                  Detected type: {getMediaTypeLabel(
                    detectedMediaType as any
                  )}
                </Typography>
              )}
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
            {detectedMediaType && (
              <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 2 }}>
                Type: {getMediaTypeLabel(detectedMediaType as any)}
              </Typography>
            )}
            <MediaPreview media={previewMedia} mediaType={previewMedia.type} />
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
                ? 'Drop your file here'
                : 'Drag and drop any file here'}
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
              Supported formats: Images, Videos, Audio, Documents, Archives
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Max size: {(MAX_FILE_SIZE / 1024 / 1024).toFixed(0)} MB
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ mt: 1, fontStyle: 'italic' }}>
              File type auto-detected - no selection needed
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
