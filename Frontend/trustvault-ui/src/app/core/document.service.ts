import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminActivityData, DashboardData, DocumentItem, DocumentPermission, UserOption } from './models';
import { API_BASE_URL } from './api';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);

  list(): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(`${API_BASE_URL}/document`);
  }

  search(filters: { name: string; owner: string; category: string; date: string }): Observable<DocumentItem[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value.trim()) params = params.set(key, value.trim());
    }
    return this.http.get<DocumentItem[]>(`${API_BASE_URL}/document/search`, { params });
  }

  dashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${API_BASE_URL}/dashboard`);
  }

  upload(title: string, category: string, file: File): Observable<DocumentItem> {
    const data = new FormData();
    data.append('title', title);
    data.append('category', category);
    data.append('file', file);
    return this.http.post<DocumentItem>(`${API_BASE_URL}/document/upload`, data);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/document/${id}`);
  }

  update(id: number, title: string, category: string): Observable<unknown> {
    return this.http.put(`${API_BASE_URL}/document/${id}`, { title, category });
  }

  view(id: number): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}/document/${id}/view`, { responseType: 'blob' });
  }

  download(id: number): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}/document/download/${id}`, { responseType: 'blob' });
  }

  usersForDocument(id: number): Observable<UserOption[]> {
    return this.http.get<UserOption[]>(`${API_BASE_URL}/permissions/users?documentId=${id}`);
  }

  permissions(id: number): Observable<DocumentPermission[]> {
    return this.http.get<DocumentPermission[]>(`${API_BASE_URL}/permissions/document/${id}`);
  }

  grant(id: number, userId: number, permission: 'READ' | 'EDIT'): Observable<unknown> {
    return this.http.post(`${API_BASE_URL}/permissions/grant`, { documentId: id, userId, permission });
  }

  revoke(id: number, userId: number): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/permissions/revoke`, { documentId: id, userId });
  }

  adminActivity(): Observable<AdminActivityData> {
    return this.http.get<AdminActivityData>(`${API_BASE_URL}/admin/activity`);
  }
}
