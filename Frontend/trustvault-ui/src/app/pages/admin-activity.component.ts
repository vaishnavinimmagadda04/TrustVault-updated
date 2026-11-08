import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { DocumentService } from '../core/document.service';
import { ActivityItem, AdminActivityData } from '../core/models';

@Component({
  standalone: true,
  imports: [DatePipe, NgTemplateOutlet],
  template: `
    <section class="page-content">
      <div class="page-heading"><div><span class="eyebrow">ADMINISTRATION</span><h1>Activity monitor</h1><p class="muted">Security events and document activity across TrustVault.</p></div></div>
      @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
      @if (data) {
        <div class="activity-grid">
          <section class="section-block activity-section"><div class="section-heading"><div><span class="eyebrow">UPLOADS</span><h2>Recent uploads</h2></div></div><ng-container [ngTemplateOutlet]="rows" [ngTemplateOutletContext]="{$implicit: data.recentUploads}" /></section>
          <section class="section-block activity-section"><div class="section-heading"><div><span class="eyebrow">DOWNLOADS</span><h2>Recent downloads</h2></div></div><ng-container [ngTemplateOutlet]="rows" [ngTemplateOutletContext]="{$implicit: data.recentDownloads}" /></section>
          <section class="section-block activity-section"><div class="section-heading"><div><span class="eyebrow">PERMISSIONS</span><h2>Access changes</h2></div></div><ng-container [ngTemplateOutlet]="rows" [ngTemplateOutletContext]="{$implicit: data.permissionChanges}" /></section>
          <section class="section-block activity-section"><div class="section-heading"><div><span class="eyebrow">DENIED REQUESTS</span><h2>Failed access attempts</h2></div></div><ng-container [ngTemplateOutlet]="rows" [ngTemplateOutletContext]="{$implicit: data.failedAccessAttempts}" /></section>
        </div>
      }
      <ng-template #rows let-items><div class="table-wrap"><table><thead><tr><th>USER</th><th>DOCUMENT / EVENT</th><th>RESULT</th><th>TIME</th></tr></thead><tbody>@for (item of items; track item.logId) { <tr><td>{{ item.userName }}</td><td><span class="table-title">{{ item.documentTitle }}<small>{{ item.action }}{{ item.details ? ' · ' + item.details : '' }}</small></span></td><td>{{ item.wasAllowed ? 'Allowed' : 'Denied' }}</td><td>{{ item.timestamp | date:'MMM d, h:mm a' }}</td></tr> }</tbody></table>@if (!items.length) { <p class="activity-empty">No events recorded.</p> }</div></ng-template>
    </section>
  `
})
export class AdminActivityComponent implements OnInit {
  private readonly service = inject(DocumentService);
  data: AdminActivityData | null = null;
  error = '';

  ngOnInit(): void {
    this.service.adminActivity().subscribe({
      next: data => this.data = data,
      error: error => this.error = error.status === 403 ? 'Administrator access is required.' : 'Could not load activity.'
    });
  }
}
