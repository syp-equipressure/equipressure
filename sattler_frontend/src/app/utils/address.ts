export interface AddressParts {
  street: string;
  postalCode: string;
  city: string;
}

/**
 * Builds the address text "Straße, PLZ Ort" from the single form fields, without a street ", PLZ Ort".
 * Returns an empty text if nothing was entered.
 */
export function composeAddress(street: string, postalCode: string, city: string): string {
  const place = [postalCode.trim(), city.trim()].filter(Boolean).join(' ');
  if (!street.trim() && !place) return '';
  return `${street.trim()}, ${place}`;
}

/** Splits "Straße, PLZ Ort" into the single form fields again. */
export function splitAddress(value: string | undefined): AddressParts {
  const text = value?.trim() ?? '';
  const match = /^(.*?),\s*(\d{4,10})\s+(.+)$/.exec(text);
  if (match) return { street: match[1].trim(), postalCode: match[2], city: match[3].trim() };
  return { street: text, postalCode: '', city: '' };
}

/** Error message for the address fields, null if they are valid. [required] means PLZ and Ort are mandatory. */
export function addressError(parts: AddressParts, required: boolean, streetRequired = false): string | null {
  const hasAnyInput = Boolean(parts.street.trim() || parts.postalCode.trim() || parts.city.trim());
  if (!required && !hasAnyInput) return null;
  if (streetRequired && !parts.street.trim()) return 'Bitte Straße und Hausnummer angeben.';
  if (!/^\d{4,10}$/.test(parts.postalCode.trim())) return 'Bitte eine gültige PLZ (nur Ziffern) angeben.';
  if (!parts.city.trim()) return 'Bitte den Ort angeben.';
  return null;
}
