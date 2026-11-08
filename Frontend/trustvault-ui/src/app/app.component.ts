import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'tv-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (auth.session; as session) {
      <div class="workspace">
        <aside class="sidebar">
          <a class="brand" routerLink="/" aria-label="TrustVault home"><span class="brand-mark">T</span><span>trustvault</span></a>
          <div class="workspace-label">WORKSPACE</div>
          <nav aria-label="Main navigation">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}"><span class="nav-icon">◫</span>Overview</a>
            <a routerLink="/documents" routerLinkActive="active"><span class="nav-icon">▤</span>My documents</a>
            @if (session.role !== 'Viewer') {
              <a routerLink="/upload" routerLinkActive="active"><span class="nav-icon">＋</span>Upload file</a>
            }
            @if (session.role === 'Admin') {
              <a routerLink="/admin/activity" routerLinkActive="active"><span class="nav-icon">◷</span>Activity monitor</a>
            }
          </nav>
          <div class="sidebar-bottom">
            <div class="user-chip"><span class="avatar">{{ session.name.slice(0, 1).toUpperCase() }}</span><span class="user-meta"><strong>{{ session.name }}</strong><small>{{ session.role }}</small></span></div>
            <button class="logout" type="button" (click)="logout()" title="Sign out"><span>↗</span> Sign out</button>
          </div>
        </aside>
        <main class="main-area"><header class="topbar"><span>Secure document workspace</span><span class="secure-indicator"><i></i> Encrypted access</span></header><router-outlet /></main>
      </div>
    } @else {
      <router-outlet />
    }
  `
})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
