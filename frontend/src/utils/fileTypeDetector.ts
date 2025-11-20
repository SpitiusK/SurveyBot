/**
 * Unified file type detection utility
 * Detects media type from file content (magic bytes), MIME type, and extension
 * Used to auto-detect file type without requiring user selection
 */

import type { MediaType } from '@/types/media';
import {
  ExtensionToMediaType,
  FileSignatures,
  AcceptedMimeTypes,
  MAX_FILE_SIZE,
} from '@/types/media';

/**
 * Reads first N bytes of a file for magic byte detection
 */
async function readFileSignature(
  file: File,
  bytes: number = 32
): Promise<Uint8Array> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const buffer = e.target?.result as ArrayBuffer;
      resolve(new Uint8Array(buffer.slice(0, bytes)));
    };
    reader.onerror = () => {
      reject(new Error('Failed to read file'));
    };
    reader.readAsArrayBuffer(file.slice(0, bytes));
  });
}

/**
 * Matches file signature against known magic bytes
 */
function matchSignature(
  signature: Uint8Array,
  patterns: number[][]
): boolean {
  return patterns.some((pattern) => {
    if (pattern.length > signature.length) return false;
    for (let i = 0; i < pattern.length; i++) {
      if (signature[i] !== pattern[i]) return false;
    }
    return true;
  });
}

/**
 * Detects media type from file extension
 */
function detectTypeFromExtension(filename: string): MediaType | null {
  const ext = '.' + filename.split('.').pop()?.toLowerCase();
  return ExtensionToMediaType[ext] || null;
}

/**
 * Detects media type from MIME type
 */
function detectTypeFromMimeType(mimeType: string): MediaType | null {
  // Check each category's MIME types
  const categories: (keyof typeof AcceptedMimeTypes)[] = [
    'image',
    'video',
    'audio',
    'document',
    'archive',
  ];

  for (const category of categories) {
    const mimeTypes = AcceptedMimeTypes[category];
    if (mimeTypes[mimeType]) {
      return category;
    }
  }

  // Fallback: check by MIME type prefix
  const prefix = mimeType.split('/')[0];
  if (prefix === 'image') return 'image';
  if (prefix === 'video') return 'video';
  if (prefix === 'audio') return 'audio';
  if (prefix === 'application') {
    // Common application subtypes
    if (
      mimeType.includes('zip') ||
      mimeType.includes('rar') ||
      mimeType.includes('7z') ||
      mimeType.includes('tar') ||
      mimeType.includes('gzip')
    ) {
      return 'archive';
    }
    return 'document';
  }
  if (prefix === 'text') return 'document';

  return null;
}

/**
 * Detects media type from file magic bytes
 */
async function detectTypeFromMagicBytes(file: File): Promise<MediaType | null> {
  try {
    const signature = await readFileSignature(file, 32);

    // Debug: Log file signature for troubleshooting
    const signatureHex = Array.from(signature.slice(0, 8))
      .map(b => '0x' + b.toString(16).toUpperCase().padStart(2, '0'))
      .join(', ');
    console.debug(`[Magic Bytes] ${file.name}: [${signatureHex}]`);

    // Check signatures for each file type
    for (const [ext, patterns] of Object.entries(FileSignatures)) {
      if (matchSignature(signature, patterns)) {
        // Map extension directly to media type using the proper format
        const extWithDot = ext.startsWith('.') ? ext : '.' + ext;
        const mediaType = ExtensionToMediaType[extWithDot];
        console.debug(`[Magic Bytes Match] ${file.name}: detected as ${ext}, mapped to ${mediaType}`);
        if (mediaType) return mediaType;
      }
    }

    console.debug(`[Magic Bytes] ${file.name}: No signature match found`);
    return null;
  } catch (error) {
    console.error('Error during magic byte detection:', error);
    return null;
  }
}

/**
 * Validates filename for security issues
 */
function validateFilename(filename: string): string | null {
  // Check for path traversal attempts
  if (
    filename.includes('..') ||
    filename.includes('/') ||
    filename.includes('\\') ||
    filename.includes('~')
  ) {
    return 'Invalid filename: path traversal detected';
  }

  // Check for null bytes
  if (filename.includes('\0')) {
    return 'Invalid filename: null bytes detected';
  }

  // Check for control characters
  if (/[\x00-\x1F\x7F]/.test(filename)) {
    return 'Invalid filename: control characters detected';
  }

  // Check length
  if (filename.length > 255) {
    return 'Filename too long (max 255 characters)';
  }

  return null;
}

/**
 * Main file type detection function
 * Strategy: Magic bytes > MIME type > Extension (fallback)
 * Returns detected media type and any errors
 */
export async function detectFileType(
  file: File
): Promise<{
  mediaType: MediaType | null;
  error: string | null;
}> {
  console.debug(`[detectFileType] Starting detection for: ${file.name} (${file.type}, ${file.size} bytes)`);

  // Validate filename
  const filenameError = validateFilename(file.name);
  if (filenameError) {
    console.warn(`[detectFileType] Filename validation failed: ${filenameError}`);
    return { mediaType: null, error: filenameError };
  }

  // Check file size
  if (file.size > MAX_FILE_SIZE) {
    const errorMsg = `File size exceeds maximum limit of ${MAX_FILE_SIZE / 1024 / 1024}MB`;
    console.warn(`[detectFileType] File size check failed: ${errorMsg}`);
    return { mediaType: null, error: errorMsg };
  }

  // Empty file check
  if (file.size === 0) {
    console.warn(`[detectFileType] File is empty`);
    return { mediaType: null, error: 'File is empty' };
  }

  // Try magic bytes detection (most reliable)
  let mediaType = await detectTypeFromMagicBytes(file);
  console.debug(`[detectFileType] Magic bytes result: ${mediaType || 'no match'}`);

  // Fallback to MIME type
  if (!mediaType && file.type) {
    console.debug(`[detectFileType] Trying MIME type: ${file.type}`);
    mediaType = detectTypeFromMimeType(file.type);
    console.debug(`[detectFileType] MIME type result: ${mediaType || 'no match'}`);
  }

  // Final fallback to extension
  if (!mediaType) {
    console.debug(`[detectFileType] Trying extension-based detection: ${file.name}`);
    mediaType = detectTypeFromExtension(file.name);
    console.debug(`[detectFileType] Extension result: ${mediaType || 'no match'}`);
  }

  // If still no match, this is unsupported file type
  if (!mediaType) {
    const errorMsg = `Unsupported file type: ${file.name}`;
    console.warn(`[detectFileType] Detection failed: ${errorMsg}`);
    return { mediaType: null, error: errorMsg };
  }

  console.debug(`[detectFileType] Success: ${file.name} detected as ${mediaType}`);
  return { mediaType, error: null };
}

/**
 * Gets human-readable media type label
 */
export function getMediaTypeLabel(mediaType: MediaType): string {
  const labels: Record<MediaType, string> = {
    image: 'Image',
    video: 'Video',
    audio: 'Audio',
    document: 'Document',
    archive: 'Archive',
  };
  return labels[mediaType];
}

/**
 * Gets all accepted file extensions as a comma-separated string
 * Used for file input accept attribute documentation
 */
export function getAcceptedExtensions(): string {
  const exts = new Set<string>();
  Object.values(ExtensionToMediaType).forEach((mediaType) => {
    const mimeTypes = AcceptedMimeTypes[mediaType];
    Object.values(mimeTypes).forEach((extensions) => {
      extensions.forEach((ext) => exts.add(ext));
    });
  });
  return Array.from(exts).sort().join(', ');
}

/**
 * Validates file extension against allowed types
 */
export function validateFileExtension(filename: string): boolean {
  const ext = '.' + filename.split('.').pop()?.toLowerCase();
  return ExtensionToMediaType.hasOwnProperty(ext);
}
