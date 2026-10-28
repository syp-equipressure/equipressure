import { Routes } from '@angular/router';
import { StartPage } from './pages/startpage/startpage.page';
import { accountGuard, onboardingGuard } from './auth/auth.guards';

// Die Startseite wird sofort gebraucht, alle anderen Seiten werden erst beim ersten Aufruf geladen
// (kleineres Start-Bundle).
export const routes: Routes = [
  {
    path: 'onboarding',
    canActivate: [onboardingGuard],
    loadComponent: () => import('./pages/onboarding/onboarding.page').then(m => m.OnboardingPage),
  },
  {
    path: '',
    canActivate: [accountGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'startpage' },
      { path: 'startpage', component: StartPage },
      {
        path: 'customers',
        loadComponent: () => import('./pages/customers/customers.page').then(m => m.CustomersPage),
      },
      {
        path: 'customers/:customerId',
        loadComponent: () => import('./pages/customers/customer-profile.page').then(m => m.CustomerProfilePage),
      },
      {
        path: 'customers/:customerId/horses',
        loadComponent: () => import('./pages/horses/horses.page').then(m => m.HorsesPage),
      },
      {
        path: 'customers/:customerId/horses/:horseId',
        loadComponent: () => import('./pages/measurements/measurements.page').then(m => m.MeasurementsPage),
      },
      {
        path: 'my-horses',
        loadComponent: () => import('./pages/horses/horses.page').then(m => m.HorsesPage),
      },
      {
        path: 'my-horses/:horseId',
        loadComponent: () => import('./pages/measurements/measurements.page').then(m => m.MeasurementsPage),
      },
      {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile.page').then(m => m.ProfilePage),
      },
      {
        path: 'devices',
        loadComponent: () => import('./pages/devices/devices.page').then(m => m.DevicesPage),
      },
      {
        path: 'new-measurement',
        loadComponent: () => import('./pages/stub/stub.page').then(m => m.StubPage),
        data: { title: 'Neue Messung' },
      },
      {
        path: 'new-measurement/session',
        loadComponent: () =>
          import('./pages/new-measurement/measurement-session/measurement-session.page')
            .then(m => m.MeasurementSessionPage),
      },
      {
        path: 'new-measurement/live',
        loadComponent: () =>
          import('./pages/new-measurement/live-measurement/live-measurement.page').then(m => m.LiveMeasurementPage),
      },
      {
        path: 'messages',
        loadComponent: () => import('./pages/messages/messages.page').then(m => m.MessagesPage),
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/settings/settings.page').then(m => m.SettingsPage),
      },
    ],
  },
];
