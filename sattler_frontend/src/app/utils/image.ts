/** Die Bildformate, die das Backend annimmt */
export const IMAGE_FILE_TYPES = 'image/jpeg,image/png,image/webp,image/gif';

const MAX_IMAGE_SIZE = 5 * 1024 * 1024;

/** Prüft ein ausgewähltes Bild, liefert die Fehlermeldung oder einen leeren Text. */
export function imageFileError(file: File): string {
  if (!IMAGE_FILE_TYPES.split(',').includes(file.type)) {
    return 'Bitte ein JPEG-, PNG-, WebP- oder GIF-Bild auswählen.';
  }
  if (file.size > MAX_IMAGE_SIZE) {
    return 'Das Bild darf höchstens 5 MB groß sein.';
  }
  return '';
}
