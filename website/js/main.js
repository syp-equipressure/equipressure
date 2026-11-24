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

// ===== Vorbestellung: Absenden =====
const orderSummary = document.querySelector('.order__summary');
const orderStatus = document.getElementById('order-status');
const orderDone = document.getElementById('order-done');
const orderDoneText = document.getElementById('order-done-text');
const { email: ORDER_EMAIL, endpoint: ORDER_ENDPOINT } = window.ORDER_CONFIG;

function orderText(data, lines) {
  const contact = [
    `Name: ${data.firstName} ${data.lastName}`,
    data.company ? `Sattlerei: ${data.company}` : '',
    `E-Mail: ${data.email}`,
    data.phone ? `Telefon: ${data.phone}` : '',
    `Ort: ${data.zip} ${data.city}`,
  ].filter(Boolean);

  return [
    'Vorbestellung über equipressure.com',
    '',
    ...lines.map(([label, value]) => `- ${label}: ${value}`),
    '',
    ...contact,
    data.message ? `\nNachricht:\n${data.message}` : '',
  ].join('\n');
}

async function sendOrder(data, lines) {
  if (ORDER_ENDPOINT) {
    const response = await fetch(ORDER_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...data, lines }),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return `Wir haben deine Vorbestellung erhalten und melden uns bei dir unter ${data.email}.`;
  }

  const subject = `Vorbestellung: ${PLANS[data.plan].name}`;
  window.location.href = `mailto:${ORDER_EMAIL}`
    + `?subject=${encodeURIComponent(subject)}`
    + `&body=${encodeURIComponent(orderText(data, lines))}`;
  return 'Dein E-Mail-Programm hat sich mit deiner Vorbestellung geöffnet. '
    + `Schick die E-Mail noch ab, dann melden wir uns bei dir unter ${data.email}.`;
}

function showDone(open) {
  orderForm.hidden = open;
  orderSummary.hidden = open;
  orderDone.hidden = !open;
}

orderForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  orderStatus.textContent = '';

  const data = Object.fromEntries(new FormData(orderForm));
  try {
    orderDoneText.textContent = await sendOrder(data, orderLines());
    showDone(true);
    orderDone.focus();
  } catch {
    orderStatus.textContent = 'Das hat leider nicht geklappt. Bitte versuch es noch einmal '
      + `oder schreib uns an ${ORDER_EMAIL}.`;
  }
});

document.getElementById('order-again').addEventListener('click', () => {
  orderForm.reset();
  updatePlanFields();
  renderSummary();
  showDone(false);
});
