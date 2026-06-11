import { Routes } from '@angular/router';
import { CustomersPage } from './pages/customers/customers.page';
import { HorsesPage } from './pages/horses/horses.page';
import { MeasurementsPage } from './pages/measurements/measurements.page';
import { StubPage } from './pages/stub/stub.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'customers' },
  { path: 'customers', component: CustomersPage },
  { path: 'customers/:customerId/horses', component: HorsesPage },
  { path: 'customers/:customerId/horses/:horseId', component: MeasurementsPage },
  { path: 'new-measurement', component: StubPage, data: { title: 'Neue Messung' } },
  { path: 'messages', component: StubPage, data: { title: 'Nachrichten' } },
  { path: 'settings', component: StubPage, data: { title: 'Einstellungen' } },
];
