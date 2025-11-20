import React, { useEffect, useRef } from 'react';
import DOMPurify from 'dompurify';
import type { MediaContentDto, MediaItemDto } from '../types/media';
import './RichContentRenderer.css';

interface RichContentRendererProps {
  htmlContent: string;
  mediaContent?: MediaContentDto;
  readOnly?: boolean;
  className?: string;
}

export const RichContentRenderer: React.FC<RichContentRendererProps> = ({
  htmlContent,
  mediaContent,
  readOnly = true,
  className = '',
}) => {
  const contentRef = useRef<HTMLDivElement>(null);

  // Sanitize HTML on mount and when content changes
  useEffect(() => {
    if (contentRef.current && htmlContent) {
      const cleanHtml = DOMPurify.sanitize(htmlContent, {
        ALLOWED_TAGS: [
          'p', 'div', 'span', 'br', 'hr',
          'strong', 'em', 'u', 's',
          'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
          'ul', 'ol', 'li',
          'blockquote', 'code', 'pre',
          'a', 'img',
        ],
        ALLOWED_ATTR: ['href', 'src', 'alt', 'title', 'class', 'style'],
        KEEP_CONTENT: true,
      });
      contentRef.current.innerHTML = cleanHtml;

      // Set up lazy loading for images
      setupLazyLoading(contentRef.current);
    }
  }, [htmlContent]);

  return (
    <div className={`rich-content-renderer ${className}`}>
      {/* Rendered HTML Content */}
      <div
        ref={contentRef}
        className="content-text"
        role={readOnly ? 'article' : undefined}
      />

      {/* Media Gallery */}
      {mediaContent?.items && mediaContent.items.length > 0 && (
        <div className="media-display">
          {mediaContent.items.map((media) => (
            <div key={media.id} className={`media-item media-${media.type}`}>
              {renderMediaItem(media)}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

function renderMediaItem(media: MediaItemDto): React.ReactNode {
  switch (media.type) {
    case 'image':
      return (
        <figure className="media-figure">
          {media.thumbnailPath && (
            <img
              src={media.thumbnailPath}
              alt={media.altText || media.displayName}
              title={media.displayName}
              loading="lazy"
              className="media-image"
            />
          )}
          {media.altText && (
            <figcaption className="media-caption">
              {media.altText}
            </figcaption>
          )}
        </figure>
      );

    case 'video':
      return (
        <figure className="media-figure">
          <video
            controls
            className="media-video"
            title={media.displayName}
            aria-label={media.displayName}
          >
            <source src={media.filePath} />
            Your browser does not support the video tag.
          </video>
          {media.altText && (
            <figcaption className="media-caption">
              {media.altText}
            </figcaption>
          )}
        </figure>
      );

    case 'audio':
      return (
        <figure className="media-figure audio">
          <audio
            controls
            className="media-audio"
            title={media.displayName}
            aria-label={media.displayName}
          >
            <source src={media.filePath} />
            Your browser does not support the audio tag.
          </audio>
          <figcaption className="media-caption">
            {media.altText || media.displayName}
          </figcaption>
        </figure>
      );

    case 'document':
      return (
        <div className="media-document">
          <a
            href={media.filePath}
            download={media.displayName}
            className="document-link"
            aria-label={`Download ${media.displayName}`}
          >
            <span className="document-icon">📄</span>
            <span className="document-name">{media.displayName}</span>
            <span className="document-size">
              ({(media.fileSize / 1024 / 1024).toFixed(2)} MB)
            </span>
          </a>
        </div>
      );

    default:
      return null;
  }
}

function setupLazyLoading(container: HTMLElement) {
  // Use Intersection Observer for lazy loading images
  if ('IntersectionObserver' in window) {
    const imageObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const img = entry.target as HTMLImageElement;
          if (img.dataset.src) {
            img.src = img.dataset.src;
            img.removeAttribute('data-src');
          }
          observer.unobserve(img);
        }
      });
    });

    container.querySelectorAll('img[data-src]').forEach((img) => {
      imageObserver.observe(img);
    });
  }
}

export default RichContentRenderer;
