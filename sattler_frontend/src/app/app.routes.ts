import { Routes } from '@angular/router';
import { CustomersPage } from './pages/customers/customers.page';
import { HorsesPage } from './pages/horses/horses.page';
import { MeasurementsPage } from './pages/measurements/measurements.page';
import { ProfilePage } from './pages/profile/profile.page';
import { MeasurementSessionPage } from './pages/new-measurement/measurement-session/measurement-session.page';
import { LiveMeasurementPage } from './pages/new-measurement/live-measurement/live-measurement.page';
import { StubPage } from './pages/stub/stub.page';
import { StartPage } from './pages/startpage/startpage.page';
import { CustomerProfilePage } from './pages/customers/customer-profile.page';
import { SettingsPage } from './pages/settings/settings.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'startpage' },
  { path: 'startpage', component: StartPage },
  { path: 'customers', component: CustomersPage },
  { path: 'customers/:customerId', component: CustomerProfilePage },
  { path: 'customers/:customerId/horses', component: HorsesPage },
  { path: 'customers/:customerId/horses/:horseId', component: MeasurementsPage },
  { path: 'my-horses', component: HorsesPage },
  { path: 'my-horses/:horseId', component: MeasurementsPage },
  { path: 'profile', component: ProfilePage },
  { path: 'new-measurement', component: StubPage, data: { title: 'Neue Messung' } },
  { path: 'new-measurement/session', component: MeasurementSessionPage },
  { path: 'new-measurement/live', component: LiveMeasurementPage },
  { path: 'messages', loadComponent: () => import('./pages/messages/messages.page').then(m => m.MessagesPage) },
  { path: 'settings', component: SettingsPage },
];
