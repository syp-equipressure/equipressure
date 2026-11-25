import * as L from 'leaflet';

// Styles: _stable-pin.scss (global, because Leaflet adds the markers outside of the components)

/** Pin with a horse, the badge shows how many customers have horses in the stable. */
export function stableIcon(customerCount: number, isSelected: boolean): L.DivIcon {
  const size: [number, number] = isSelected ? [44, 54] : [36, 44];
  return L.divIcon({
    className: 'stable-pin-wrapper',
    html: `<span class="stable-pin${isSelected ? ' is-selected' : ''}">
             <span class="stable-pin-horse"></span>
             <span class="stable-pin-count">${customerCount}</span>
           </span>`,
    iconSize: size,
    iconAnchor: [size[0] / 2, size[1]],
  });
}
