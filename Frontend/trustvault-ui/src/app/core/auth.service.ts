import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { UserSession } from './models';
import { API_BASE_URL } from './api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'trustvault.session';

  get session(): UserSession | null {
    const value = sessionStorage.getItem(this.storageKey);
    if (!value) return null;
    try {
      return JSON.parse(value) as UserSession;
    } catch {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }
  }

  login(email: string, password: string): Observable<UserSession> {
    return this.http.post<UserSession>(`${API_BASE_URL}/auth/login`, { email, password }).pipe(tap(session => this.store(session)));
  }

  register(name: string, email: string, password: string, role: 'Owner' | 'Viewer'): Observable<unknown> {
    return this.http.post(`${API_BASE_URL}/auth/register`, { name, email, password, role });
  }

  logout(): void {
    sessionStorage.removeItem(this.storageKey);
  }

  private store(session: UserSession): void {
    sessionStorage.setItem(this.storageKey, JSON.stringify(session));
  }
}
