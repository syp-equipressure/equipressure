'use strict';

// ===== Mobiles Menü =====
const navToggle = document.querySelector('.nav-toggle');
const nav = document.getElementById('nav');

function setNavOpen(open) {
  nav.classList.toggle('is-open', open);
  navToggle.setAttribute('aria-expanded', String(open));
  navToggle.setAttribute('aria-label', open ? 'Menü schließen' : 'Menü öffnen');
}

navToggle.addEventListener('click', () => {
  setNavOpen(!nav.classList.contains('is-open'));
});

// Nach dem Klick auf einen Link soll das Menü wieder zugehen.
nav.addEventListener('click', (event) => {
  if (event.target.closest('a')) setNavOpen(false);
});

document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape') setNavOpen(false);
});
