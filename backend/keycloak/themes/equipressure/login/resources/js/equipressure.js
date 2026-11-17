// Zeigt die Bezeichnungen der Eingabefelder als Platzhalter im Feld, für Screenreader bleibt das Label erhalten
document.addEventListener('DOMContentLoaded', () => {
  for (const group of document.querySelectorAll('.pf-v5-c-form__group')) {
    const input = group.querySelector('input:not([type="hidden"]):not([type="checkbox"])');
    const label = group.querySelector('.pf-v5-c-form__group-label');
    if (!input || !label) continue;

    input.placeholder = label.textContent.replace('*', '').trim();
    label.classList.add('ep-label-hidden');
  }
});
