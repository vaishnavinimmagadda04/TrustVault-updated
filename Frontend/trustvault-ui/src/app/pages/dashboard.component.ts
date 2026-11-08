import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DocumentService } from '../core/document.service';
import { DashboardData } from '../core/models';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <section class="page-content">
      <div class="page-heading"><div><span class="eyebrow">YOUR WORKSPACE</span><h1>Overview</h1><p class="muted">A clear view of your team's secure files.</p></div><a class="button primary" routerLink="/upload">＋ <span>Upload document</span></a></div>
      @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
      @if (data) {
        <div class="stats-grid">
          <article class="stat-panel"><span class="stat-label">TOTAL DOCUMENTS</span><strong>{{ data.totalDocuments }}</strong><span class="stat-caption">In your workspace</span><span class="stat-symbol">▤</span></article>
          <article class="stat-panel"><span class="stat-label">MY UPLOADS</span><strong>{{ data.myUploads }}</strong><span class="stat-caption">Added by you</span><span class="stat-symbol">↑</span></article>
          <article class="stat-panel accent"><span class="stat-label">RECENT ACTIVITY</span><strong>{{ data.recentDocuments.length }}</strong><span class="stat-caption">Latest documents shown</span><span class="stat-symbol">◷</span></article>
        </div>
        <section class="section-block"><div class="section-heading"><div><span class="eyebrow">LATEST FILES</span><h2>Recent documents</h2></div><a class="text-link" routerLink="/documents">View all <span>→</span></a></div>
          @if (data.recentDocuments.length) {
            <div class="table-wrap"><table><thead><tr><th>DOCUMENT</th><th>CATEGORY</th><th>ADDED BY</th><th>DATE ADDED</th></tr></thead><tbody>
              @for (doc of data.recentDocuments; track doc.documentId) { <tr><td><span class="file-glyph">▧</span><span class="table-title">{{ doc.title }}<small>{{ doc.originalFileName }}</small></span></td><td><span class="category-label">{{ doc.category }}</span></td><td>{{ doc.ownerName }}</td><td>{{ doc.uploadDate | date:'MMM d, y' }}</td></tr> }
            </tbody></table></div>
          } @else { <div class="empty-state"><span class="empty-icon">▤</span><h3>No documents yet</h3><p>Your uploaded files will appear here.</p><a class="text-link" routerLink="/upload">Upload your first document →</a></div> }
        </section>
      }
    </section>
  `
})
export class DashboardComponent implements OnInit {
  private readonly documents = inject(DocumentService);
  data: DashboardData | null = null;
  error = '';

  ngOnInit(): void {
    this.documents.dashboard().subscribe({
      next: data => this.data = data,
      error: () => this.error = 'Could not load the dashboard. Check that the API is running.'
    });
  }
}
