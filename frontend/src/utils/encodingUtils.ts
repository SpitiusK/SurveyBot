/**
 * Encoding utilities for CSV export
 * Provides Windows-1251 encoding for Russian Excel compatibility
 */

/**
 * Encode string to Windows-1251 (CP1251) byte array
 * For Russian text compatibility with Microsoft Excel on Windows
 *
 * Windows-1251 encoding map for Cyrillic:
 * - А-Я (U+0410-U+042F) → 0xC0-0xDF
 * - а-я (U+0430-U+044F) → 0xE0-0xFF
 * - Ё (U+0401) → 0xA8
 * - ё (U+0451) → 0xB8
 *
 * @param str - Unicode string to encode
 * @returns Uint8Array of Windows-1251 encoded bytes
 */
export function encodeWindows1251(str: string): Uint8Array {
  const bytes: number[] = [];

  for (let i = 0; i < str.length; i++) {
    const code = str.charCodeAt(i);

    if (code < 128) {
      // ASCII characters (0x00-0x7F) - same in both encodings
      bytes.push(code);
    } else if (code >= 0x0410 && code <= 0x042F) {
      // Cyrillic uppercase А-Я (U+0410-U+042F) → 0xC0-0xDF in Windows-1251
      bytes.push(code - 0x0410 + 0xC0);
    } else if (code >= 0x0430 && code <= 0x044F) {
      // Cyrillic lowercase а-я (U+0430-U+044F) → 0xE0-0xFF in Windows-1251
      bytes.push(code - 0x0430 + 0xE0);
    } else if (code === 0x0401) {
      // Ё (U+0401) → 0xA8 in Windows-1251
      bytes.push(0xA8);
    } else if (code === 0x0451) {
      // ё (U+0451) → 0xB8 in Windows-1251
      bytes.push(0xB8);
    } else if (code === 0x0404) {
      // Є (Ukrainian, U+0404) → 0xAA in Windows-1251
      bytes.push(0xAA);
    } else if (code === 0x0454) {
      // є (Ukrainian, U+0454) → 0xBA in Windows-1251
      bytes.push(0xBA);
    } else if (code === 0x0406) {
      // І (Ukrainian/Belarusian, U+0406) → 0xB2 in Windows-1251
      bytes.push(0xB2);
    } else if (code === 0x0456) {
      // і (Ukrainian/Belarusian, U+0456) → 0xB3 in Windows-1251
      bytes.push(0xB3);
    } else if (code === 0x0407) {
      // Ї (Ukrainian, U+0407) → 0xAF in Windows-1251
      bytes.push(0xAF);
    } else if (code === 0x0457) {
      // ї (Ukrainian, U+0457) → 0xBF in Windows-1251
      bytes.push(0xBF);
    } else if (code === 0x0490) {
      // Ґ (Ukrainian, U+0490) → 0xA5 in Windows-1251
      bytes.push(0xA5);
    } else if (code === 0x0491) {
      // ґ (Ukrainian, U+0491) → 0xB4 in Windows-1251
      bytes.push(0xB4);
    } else if (code === 0x040E) {
      // Ў (Belarusian, U+040E) → 0xA1 in Windows-1251
      bytes.push(0xA1);
    } else if (code === 0x045E) {
      // ў (Belarusian, U+045E) → 0xA2 in Windows-1251
      bytes.push(0xA2);
    } else if (code >= 0x2010 && code <= 0x2015) {
      // Various dashes → regular hyphen
      bytes.push(0x2D);
    } else if (code === 0x2018 || code === 0x2019) {
      // Single curly quotes → apostrophe
      bytes.push(0x27);
    } else if (code === 0x201C || code === 0x201D) {
      // Double curly quotes → straight quote
      bytes.push(0x22);
    } else if (code === 0x2026) {
      // Ellipsis → 0x85 in Windows-1251
      bytes.push(0x85);
    } else if (code === 0x20AC) {
      // Euro sign → 0x88 in Windows-1251
      bytes.push(0x88);
    } else if (code === 0x2116) {
      // № (numero sign, U+2116) → 0xB9 in Windows-1251
      bytes.push(0xB9);
    } else {
      // Unknown characters - use ? as fallback
      bytes.push(0x3F);
    }
  }

  return new Uint8Array(bytes);
}
