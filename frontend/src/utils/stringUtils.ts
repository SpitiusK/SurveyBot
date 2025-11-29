/**
 * String utility functions for text processing
 */

/**
 * Strip HTML tags from a string and return plain text content
 * @param html - String containing HTML markup
 * @returns Plain text content without HTML tags
 */
export const stripHtml = (html: string): string => {
  if (!html) return '';

  const tmp = document.createElement('div');
  tmp.innerHTML = html;
  return (tmp.textContent || tmp.innerText || '').trim();
};

/**
 * Truncate text to a maximum length and add ellipsis if needed
 * @param text - Text to truncate
 * @param maxLength - Maximum length before truncation
 * @returns Truncated text with ellipsis if needed
 */
export const truncate = (text: string, maxLength: number): string => {
  if (!text || text.length <= maxLength) return text;
  return text.substring(0, maxLength) + '...';
};

/**
 * Strip HTML tags and truncate in one operation
 * @param html - HTML string to process
 * @param maxLength - Maximum length after HTML stripping
 * @returns Plain text, truncated if necessary
 */
export const stripHtmlAndTruncate = (html: string, maxLength: number): string => {
  const plainText = stripHtml(html);
  return truncate(plainText, maxLength);
};
