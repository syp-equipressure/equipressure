'use strict';

// Einstellungen für die Vorbestellung.
// - endpoint leer:  Das Formular öffnet das E-Mail-Programm mit der fertigen
//                   Vorbestellung an "email".
// - endpoint gesetzt: Die Vorbestellung wird als JSON per POST dorthin geschickt.
// TODO: echte Adresse eintragen, sobald feststeht, wer die Vorbestellungen bekommt.
window.ORDER_CONFIG = {
  email: 'office@equipressure.com',
  endpoint: '',
};
