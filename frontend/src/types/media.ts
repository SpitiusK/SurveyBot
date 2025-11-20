// Media Types for Multimedia Support - Unified File Upload System
// Supports all Telegram-compatible file types

export type MediaType = 'image' | 'video' | 'audio' | 'document' | 'archive';

export interface MediaItemDto {
  id: string;
  type: MediaType;
  filePath: string;
  displayName: string;
  fileSize: number;
  mimeType: string;
  uploadedAt: string;
  altText?: string;
  thumbnailPath?: string;
  order: number;
}

export interface MediaContentDto {
  version: string;
  items: MediaItemDto[];
}

export interface MediaValidationError {
  field: string;
  message: string;
}

export interface UploadProgress {
  isUploading: boolean;
  progress: number; // 0-100
  fileName?: string;
  error?: string;
}

// Maximum file size for unified upload: 100 MB (Telegram limit)
export const MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB

// Legacy file size limits by type (for backward compatibility)
export const MediaFileSizeLimits: Record<MediaType, number> = {
  image: 10 * 1024 * 1024,    // 10 MB
  video: 50 * 1024 * 1024,    // 50 MB
  audio: 20 * 1024 * 1024,    // 20 MB
  document: 25 * 1024 * 1024, // 25 MB
  archive: 100 * 1024 * 1024, // 100 MB
};

/**
 * Unified MIME type mapping for all supported file types
 * Used for file input accept attribute and validation
 */
export const AcceptedMimeTypes: Record<MediaType, Record<string, string[]>> = {
  // Images: All common formats
  image: {
    'image/jpeg': ['.jpg', '.jpeg'],
    'image/png': ['.png'],
    'image/gif': ['.gif'],
    'image/webp': ['.webp'],
    'image/bmp': ['.bmp'],
    'image/tiff': ['.tiff', '.tif'],
    'image/x-icon': ['.ico'],
    'image/svg+xml': ['.svg'],
  },
  // Videos: All common formats including those that might have incorrect MIME types
  video: {
    'video/mp4': ['.mp4'],
    'video/webm': ['.webm'],
    'video/quicktime': ['.mov'],
    'video/x-msvideo': ['.avi'],
    'video/x-matroska': ['.mkv'],
    'video/x-flv': ['.flv'],
    'video/x-ms-wmv': ['.wmv'],
    'video/3gpp': ['.3gp'],
    'video/x-ms-asf': ['.asf'],
    'video/mpeg': ['.mpeg', '.mpg'],
    'application/x-mpegURL': ['.m3u8'],
  },
  // Audio: All common formats
  audio: {
    'audio/mpeg': ['.mp3'],
    'audio/wav': ['.wav'],
    'audio/ogg': ['.ogg', '.oga'],
    'audio/mp4': ['.m4a'],
    'audio/flac': ['.flac'],
    'audio/aac': ['.aac'],
    'audio/x-ms-wma': ['.wma'],
    'audio/x-caf': ['.caf'],
    'audio/webp': ['.weba'],
    'audio/x-wav': ['.wav'],
    'audio/aiff': ['.aiff', '.aif'],
  },
  // Documents: All common office and text formats
  document: {
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
    'text/x-markdown': ['.md'],
    'application/epub+zip': ['.epub'],
  },
  // Archives: All common compression formats
  archive: {
    'application/zip': ['.zip'],
    'application/x-rar-compressed': ['.rar'],
    'application/x-7z-compressed': ['.7z'],
    'application/x-tar': ['.tar'],
    'application/gzip': ['.gz', '.gzip'],
    'application/x-bzip2': ['.bz2'],
    'application/x-xz': ['.xz'],
    'application/x-lzip': ['.lz'],
    'application/x-lzma': ['.lzma'],
    'application/x-snappy-framed': ['.snappy'],
  },
};

/**
 * File signature (magic bytes) for detecting actual file type
 * Used to validate that file content matches extension
 */
export const FileSignatures: Record<string, number[][]> = {
  // Images
  jpg: [[0xFF, 0xD8, 0xFF]],
  png: [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
  gif: [[0x47, 0x49, 0x46, 0x38]],
  bmp: [[0x42, 0x4D]],
  ico: [[0x00, 0x00, 0x01, 0x00]],
  webp: [[0x52, 0x49, 0x46, 0x46]],
  // Videos
  mp4: [[0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70], [0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70], [0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70]],
  webm: [[0x1A, 0x45, 0xDF, 0xA3]],
  avi: [[0x52, 0x49, 0x46, 0x46]],
  mkv: [[0x1A, 0x45, 0xDF, 0xA3]],
  flv: [[0x46, 0x4C, 0x56]],
  mov: [[0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70]],
  // Audio
  mp3: [[0xFF, 0xFB], [0xFF, 0xF3], [0xFF, 0xF2], [0x49, 0x44, 0x33]],
  wav: [[0x52, 0x49, 0x46, 0x46]],
  ogg: [[0x4F, 0x67, 0x67, 0x53]],
  flac: [[0x66, 0x4C, 0x61, 0x43]],
  m4a: [[0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70]],
  // Documents
  pdf: [[0x25, 0x50, 0x44, 0x46]],
  zip: [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
  rar: [[0x52, 0x61, 0x72, 0x21, 0x1A, 0x07]],
  '7z': [[0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]],
  tar: [[0x75, 0x73, 0x74, 0x61, 0x72]],
  gzip: [[0x1F, 0x8B]],
  bzip2: [[0x42, 0x5A]],
};

/**
 * Extension to media type mapping for unified upload
 * Used when MIME type detection fails or is ambiguous
 */
export const ExtensionToMediaType: Record<string, MediaType> = {
  // Images
  '.jpg': 'image',
  '.jpeg': 'image',
  '.png': 'image',
  '.gif': 'image',
  '.webp': 'image',
  '.bmp': 'image',
  '.tiff': 'image',
  '.tif': 'image',
  '.ico': 'image',
  '.svg': 'image',
  // Videos
  '.mp4': 'video',
  '.webm': 'video',
  '.mov': 'video',
  '.avi': 'video',
  '.mkv': 'video',
  '.flv': 'video',
  '.wmv': 'video',
  '.3gp': 'video',
  '.asf': 'video',
  '.mpeg': 'video',
  '.mpg': 'video',
  '.m3u8': 'video',
  // Audio
  '.mp3': 'audio',
  '.wav': 'audio',
  '.ogg': 'audio',
  '.oga': 'audio',
  '.m4a': 'audio',
  '.flac': 'audio',
  '.aac': 'audio',
  '.wma': 'audio',
  '.caf': 'audio',
  '.weba': 'audio',
  '.aiff': 'audio',
  '.aif': 'audio',
  // Documents
  '.pdf': 'document',
  '.doc': 'document',
  '.docx': 'document',
  '.xls': 'document',
  '.xlsx': 'document',
  '.ppt': 'document',
  '.pptx': 'document',
  '.txt': 'document',
  '.rtf': 'document',
  '.odt': 'document',
  '.ods': 'document',
  '.odp': 'document',
  '.csv': 'document',
  '.json': 'document',
  '.xml': 'document',
  '.md': 'document',
  '.epub': 'document',
  // Archives
  '.zip': 'archive',
  '.rar': 'archive',
  '.7z': 'archive',
  '.tar': 'archive',
  '.gz': 'archive',
  '.gzip': 'archive',
  '.bz2': 'archive',
  '.xz': 'archive',
  '.lz': 'archive',
  '.lzma': 'archive',
  '.snappy': 'archive',
};

/**
 * Helper function to get accepted MIME types and extensions for all file types
 * Returns the AcceptedMimeTypes object that can be used with react-dropzone
 */
export function getAcceptedExtensions(): Record<string, string[]> {
  const result: Record<string, string[]> = {};

  // Flatten AcceptedMimeTypes into a single object for dropzone
  Object.values(AcceptedMimeTypes).forEach(mimeTypeGroup => {
    Object.entries(mimeTypeGroup).forEach(([mimeType, extensions]) => {
      result[mimeType] = extensions;
    });
  });

  return result;
}
