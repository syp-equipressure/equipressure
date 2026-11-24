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

// ===== Vorbestellung: Paket wählen =====
const orderForm = document.getElementById('order-form');

function selectedPlan() {
  return orderForm.elements.plan.value;
}

// Zeigt nur die Felder, die zum gewählten Paket gehören.
// Ausgeblendete Felder werden deaktiviert, damit sie nicht mitgeschickt werden.
function updatePlanFields() {
  const plan = selectedPlan();
  orderForm.querySelectorAll('[data-for]').forEach((el) => {
    const visible = el.dataset.for === plan;
    el.hidden = !visible;
    el.querySelectorAll('input, textarea').forEach((input) => { input.disabled = !visible; });
  });
}

orderForm.addEventListener('change', (event) => {
  if (event.target.name === 'plan') updatePlanFields();
});

// Die Buttons bei den Preisen wählen das passende Paket schon vor.
document.querySelectorAll('a[data-plan]').forEach((link) => {
  link.addEventListener('click', () => {
    orderForm.elements.plan.value = link.dataset.plan;
    updatePlanFields();
  });
});

updatePlanFields();
