import { Routes } from '@angular/router';
import { Inbox } from './features/inbox/inbox';

export const routes: Routes = [
  { path: '', component: Inbox },
  { path: '**', redirectTo: '' },
];
