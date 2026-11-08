import { Routes } from '@angular/router';
import { authGuard, uploadGuard } from './core/auth.guard';
import { AuthPageComponent } from './pages/auth-page.component';
import { DashboardComponent } from './pages/dashboard.component';
import { DocumentsComponent } from './pages/documents.component';
import { UploadComponent } from './pages/upload.component';
import { ManageAccessComponent } from './pages/manage-access.component';
import { AdminActivityComponent } from './pages/admin-activity.component';

export const routes: Routes = [
  { path: 'login', component: AuthPageComponent, data: { mode: 'login' } },
  { path: 'register', component: AuthPageComponent, data: { mode: 'register' } },
  { path: '', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'documents', component: DocumentsComponent, canActivate: [authGuard] },
  { path: 'upload', component: UploadComponent, canActivate: [authGuard, uploadGuard] },
  { path: 'documents/:id/access', component: ManageAccessComponent, canActivate: [authGuard] },
  { path: 'admin/activity', component: AdminActivityComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
