import { imageFileError } from './image';

describe('imageFileError', () => {
  function file(type: string, size = 10): File {
    return new File([new Uint8Array(size)], 'bild', { type });
  }

  it('accepts the formats of the backend', () => {
    for (const type of ['image/jpeg', 'image/png', 'image/webp', 'image/gif']) {
      expect(imageFileError(file(type))).toBe('');
    }
  });

  it('rejects other formats like SVG', () => {
    expect(imageFileError(file('image/svg+xml'))).not.toBe('');
    expect(imageFileError(file('application/pdf'))).not.toBe('');
  });

  it('rejects images above 5 MB', () => {
    expect(imageFileError(file('image/png', 5 * 1024 * 1024))).toBe('');
    expect(imageFileError(file('image/png', 5 * 1024 * 1024 + 1))).not.toBe('');
  });
});
