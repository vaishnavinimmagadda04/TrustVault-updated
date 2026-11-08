import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { DocumentService } from '../core/document.service';
import { DocumentItem } from '../core/models';

@Component({
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink],
  template: `
    <section class="page-content">
      <div class="page-heading"><div><span class="eyebrow">SECURE ARCHIVE</span><h1>My documents</h1><p class="muted">Browse and manage your stored files.</p></div>@if (auth.session?.role !== 'Viewer') { <a class="button primary" routerLink="/upload">＋ <span>Upload document</span></a> }</div>
      @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
      <form class="document-search" (ngSubmit)="search()">
        <label>Document name<input name="name" [(ngModel)]="filters.name" placeholder="Search title or filename"></label>
        <label>Owner<input name="owner" [(ngModel)]="filters.owner" placeholder="Search owner"></label>
        <label>Category<input name="category" [(ngModel)]="filters.category" placeholder="Search category"></label>
        <label>Upload date<input name="date" type="date" [(ngModel)]="filters.date"></label>
        <div class="search-actions"><button class="button primary" type="submit">Search</button><button class="text-link" type="button" (click)="clearSearch()">Clear</button></div>
      </form>
      <section class="section-block document-list"><div class="section-heading"><div><span class="eyebrow">DOCUMENT LIBRARY</span><h2>{{ documents.length }} {{ documents.length === 1 ? 'file' : 'files' }}</h2></div></div>
        @if (documents.length) {
          <div class="table-wrap"><table><thead><tr><th>DOCUMENT</th><th>CATEGORY</th><th>ACCESS</th><th>UPLOADED</th><th>OWNER</th><th class="actions-heading">ACTIONS</th></tr></thead><tbody>
            @for (doc of documents; track doc.documentId) { <tr><td><span class="file-glyph">▧</span><span class="table-title">{{ doc.title }}<small>{{ doc.originalFileName }}</small></span></td><td><span class="category-label">{{ doc.category }}</span></td><td><span class="category-label">{{ doc.accessLevel }}</span></td><td>{{ doc.uploadDate | date:'MMM d, y' }}</td><td>{{ doc.ownerName }}</td><td class="row-actions"><button class="icon-action" type="button" title="Open document" [attr.aria-label]="'Open ' + doc.title" (click)="open(doc)">↗</button><button class="icon-action" type="button" title="Download document" [attr.aria-label]="'Download ' + doc.title" (click)="download(doc)">↓</button>@if (canEdit(doc)) { <button class="icon-action" type="button" title="Edit details" [attr.aria-label]="'Edit ' + doc.title" (click)="edit(doc)">✎</button> }@if (canManage(doc)) { <a class="icon-action" [routerLink]="['/documents', doc.documentId, 'access']" title="Manage access" [attr.aria-label]="'Manage access to ' + doc.title">◇</a> }@if (auth.session?.role === 'Admin' || auth.session?.userId === doc.ownerId) { <button class="icon-action danger" type="button" title="Delete document" [attr.aria-label]="'Delete ' + doc.title" (click)="remove(doc)">×</button> }</td></tr> }
          </tbody></table></div>
        } @else if (searched) { <div class="empty-state"><span class="empty-icon">⌕</span><h3>No matching documents</h3><p>Try changing or clearing your search filters.</p></div>
        } @else { <div class="empty-state"><span class="empty-icon">▤</span><h3>Your library is empty</h3><p>When you upload a file, it will be available here.</p>@if (auth.session?.role !== 'Viewer') { <a class="text-link" routerLink="/upload">Upload a document →</a> }</div> }
      </section>
    </section>
  `
})
export class DocumentsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly service = inject(DocumentService);
  documents: DocumentItem[] = [];
  error = '';
  searched = false;
  filters = { name: '', owner: '', category: '', date: '' };

  ngOnInit(): void { this.load(); }

  search(): void {
    this.searched = true;
    this.service.search(this.filters).subscribe({
      next: documents => { this.documents = documents; this.error = ''; },
      error: () => this.error = 'Could not search documents. Check that the API is running.'
    });
  }

  clearSearch(): void {
    this.filters = { name: '', owner: '', category: '', date: '' };
    this.searched = false;
    this.load();
  }

  open(item: DocumentItem): void {
    const preview = window.open('about:blank', '_blank');
    this.service.view(item.documentId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        if (preview) preview.location.replace(url);
        else {
          const link = document.createElement('a');
          link.href = url;
          link.download = item.originalFileName;
          link.click();
        }
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => { preview?.close(); this.error = 'Could not open this document.'; }
    });
  }

  download(item: DocumentItem): void {
    this.service.download(item.documentId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = item.originalFileName;
        link.click();
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => this.error = 'Download denied or unavailable.'
    });
  }

  edit(item: DocumentItem): void {
    const title = window.prompt('Document title', item.title)?.trim();
    if (!title) return;
    const category = window.prompt('Category', item.category)?.trim();
    if (!category) return;
    this.service.update(item.documentId, title, category).subscribe({
      next: () => this.load(),
      error: () => this.error = 'Edit denied or unavailable.'
    });
  }

  canEdit(item: DocumentItem): boolean {
    return item.accessLevel === 'OWNER' || item.accessLevel === 'ADMIN' || item.accessLevel === 'EDIT';
  }

  canManage(item: DocumentItem): boolean {
    return this.auth.session?.role === 'Admin' || this.auth.session?.userId === item.ownerId;
  }

  remove(item: DocumentItem): void {
    if (!window.confirm(`Delete “${item.title}”? This cannot be undone.`)) return;
    this.service.remove(item.documentId).subscribe({
      next: () => this.load(),
      error: () => this.error = 'Could not delete this document.'
    });
  }

  private load(): void {
    this.service.list().subscribe({
      next: documents => { this.documents = documents; this.error = ''; },
      error: () => this.error = 'Could not load documents. Check that the API is running.'
    });
  }
}
