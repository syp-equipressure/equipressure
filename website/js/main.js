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

// ===== Vorbestellung: Übersicht =====
const PLANS = {
  reiter: { name: 'Messgerät mit Reiter-App', price: 'auf Anfrage' },
  sattler: { name: 'Messgerät mit Sattler-Web-App', price: 'auf Anfrage' },
};

function count(name) {
  const value = Number.parseInt(orderForm.elements[name].value, 10);
  return Number.isNaN(value) ? 0 : value;
}

// Positionen der Vorbestellung als [Bezeichnung, Wert]
function orderLines() {
  const plan = selectedPlan();
  const lines = [[`${count('devices')} × ${PLANS[plan].name}`, PLANS[plan].price]];

  if (plan === 'reiter') {
    if (count('extraHorses') > 0) lines.push([`${count('extraHorses')} × zusätzlicher Pferdeplatz`, 'auf Anfrage']);
    if (count('extraRiders') > 0) lines.push([`${count('extraRiders')} × zusätzlicher Reiterplatz`, 'auf Anfrage']);
    if (orderForm.elements.extraMeasurements.checked) lines.push(['Zusätzliche Messungen', 'auf Anfrage']);
    lines.push(['Reiter-App', 'inklusive']);
  } else {
    lines.push(['Sattler-Web-App', '20 € / Monat']);
  }
  return lines;
}

const summaryList = document.getElementById('summary-list');

function renderSummary() {
  summaryList.replaceChildren(...orderLines().map(([label, value]) => {
    const li = document.createElement('li');
    const left = document.createElement('span');
    const right = document.createElement('span');
    left.textContent = label;
    right.textContent = value;
    li.append(left, right);
    return li;
  }));
}

orderForm.addEventListener('input', renderSummary);
orderForm.addEventListener('change', renderSummary);
document.querySelectorAll('a[data-plan]').forEach((link) => link.addEventListener('click', renderSummary));
renderSummary();
