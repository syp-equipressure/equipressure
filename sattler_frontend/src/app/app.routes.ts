import { Routes } from '@angular/router';
import { CustomersPage } from './pages/customers/customers.page';
import { HorsesPage } from './pages/horses/horses.page';
import { MeasurementsPage } from './pages/measurements/measurements.page';
import { ProfilePage } from './pages/profile/profile.page';
import { MessagesPage } from './pages/messages/messages.page';
import { MeasurementSessionPage } from './pages/new-measurement/measurement-session/measurement-session.page';
import { LiveMeasurementPage } from './pages/new-measurement/live-measurement/live-measurement.page';
import { StubPage } from './pages/stub/stub.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'customers' },
  { path: 'customers', component: CustomersPage },
  { path: 'customers/:customerId/horses', component: HorsesPage },
  { path: 'customers/:customerId/horses/:horseId', component: MeasurementsPage },
  { path: 'profile', component: ProfilePage },
  { path: 'new-measurement', component: StubPage, data: { title: 'Neue Messung' } },
  { path: 'new-measurement/session', component: MeasurementSessionPage },
  { path: 'new-measurement/live', component: LiveMeasurementPage },
  { path: 'messages', component: MessagesPage },
  { path: 'settings', component: StubPage, data: { title: 'Einstellungen' } },
];
