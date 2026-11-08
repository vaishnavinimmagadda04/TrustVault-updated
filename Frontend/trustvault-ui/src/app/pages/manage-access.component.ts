import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DocumentService } from '../core/document.service';
import { DocumentItem, DocumentPermission, UserOption } from '../core/models';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <section class="page-content narrow-page">
      <div class="page-heading"><div><span class="eyebrow">DOCUMENT SECURITY</span><h1>Manage access</h1><p class="muted">{{ document?.title ?? 'Document access' }} · {{ document?.originalFileName ?? '' }}</p></div><a class="text-link" routerLink="/documents">← Back to documents</a></div>
      @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
      @if (message) { <p class="form-success" role="status">{{ message }}</p> }
      <section class="upload-form access-form">
        <div class="section-heading"><div><span class="eyebrow">GRANT PERMISSION</span><h2>Share with a person</h2></div></div>
        <div class="access-controls">
          <label>User<select [value]="selectedUserId" (change)="selectedUserId = $any($event.target).value"><option value="" disabled>Select a user</option>@for (user of users; track user.userId) { <option [value]="user.userId">{{ user.name }} · {{ user.email }}</option> }</select></label>
          <label>Permission<select [value]="permission" (change)="permission = $any($event.target).value"><option value="READ">READ</option><option value="EDIT">EDIT</option></select></label>
          <button class="button primary" type="button" [disabled]="!selectedUserId || busy" (click)="grant()">Grant access <span>→</span></button>
        </div>
      </section>
      <section class="section-block"><div class="section-heading"><div><span class="eyebrow">CURRENT AND REVOKED ACCESS</span><h2>People with access</h2></div></div>
        @if (permissions.length) {
          <div class="table-wrap"><table><thead><tr><th>PERSON</th><th>PERMISSION</th><th>GRANTED</th><th>STATUS</th><th></th></tr></thead><tbody>
            @for (item of permissions; track item.permissionId) { <tr><td><span class="table-title">{{ item.userName }}<small>{{ item.email }}</small></span></td><td><span class="category-label">{{ item.permission }}</span></td><td>{{ item.grantedDate | date:'MMM d, y, h:mm a' }}</td><td>{{ item.isActive ? 'Active' : 'Revoked' }}</td><td class="row-actions">@if (item.isActive) { <button class="text-link revoke-link" type="button" [disabled]="busy" (click)="revoke(item)">Revoke</button> }</td></tr> }
          </tbody></table></div>
        } @else { <div class="empty-state"><span class="empty-icon">◇</span><h3>No permissions granted</h3><p>Only the owner can access this document until someone is added.</p></div> }
      </section>
    </section>
  `
})
export class ManageAccessComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(DocumentService);
  readonly documentId = Number(this.route.snapshot.paramMap.get('id'));
  document: DocumentItem | undefined;
  users: UserOption[] = [];
  permissions: DocumentPermission[] = [];
  selectedUserId = '';
  permission: 'READ' | 'EDIT' = 'READ';
  error = '';
  message = '';
  busy = false;

  ngOnInit(): void {
    this.service.list().subscribe({
      next: items => this.document = items.find(item => item.documentId === this.documentId),
      error: () => this.error = 'Could not load the document.'
    });
    this.loadAccess();
  }

  grant(): void {
    if (!this.selectedUserId || this.busy) return;
    this.busy = true;
    this.error = '';
    this.message = '';
    this.service.grant(this.documentId, Number(this.selectedUserId), this.permission).subscribe({
      next: () => { this.busy = false; this.message = 'Access granted.'; this.selectedUserId = ''; this.loadAccess(); },
      error: error => { this.busy = false; this.error = error.status === 403 ? 'Only the document owner or an administrator can manage access.' : error.error?.message ?? 'Could not grant access.'; }
    });
  }

  revoke(item: DocumentPermission): void {
    this.busy = true;
    this.error = '';
    this.message = '';
    this.service.revoke(this.documentId, item.userId).subscribe({
      next: () => { this.busy = false; this.message = `Access revoked for ${item.userName}.`; this.loadAccess(); },
      error: error => { this.busy = false; this.error = error.status === 403 ? 'Only the document owner or an administrator can manage access.' : error.error?.message ?? 'Could not revoke access.'; }
    });
  }

  private loadAccess(): void {
    this.service.usersForDocument(this.documentId).subscribe({
      next: users => this.users = users,
      error: error => this.error = error.status === 403 ? 'Only the document owner or an administrator can manage access.' : 'Could not load users.'
    });
    this.service.permissions(this.documentId).subscribe({
      next: permissions => this.permissions = permissions,
      error: error => this.error = error.status === 403 ? 'Only the document owner or an administrator can manage access.' : 'Could not load permissions.'
    });
  }
}
