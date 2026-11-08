import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DocumentService } from '../core/document.service';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="page-content narrow-page">
      <div class="page-heading"><div><span class="eyebrow">ADD TO YOUR ARCHIVE</span><h1>Upload document</h1><p class="muted">Keep your important files organized and secure.</p></div></div>
      <form class="upload-form" [formGroup]="form" (ngSubmit)="submit()">
        <label>Document name<input formControlName="title" maxlength="200" placeholder="e.g. Vendor agreement"></label>
        <label>Category<select formControlName="category"><option value="" disabled>Select category</option>@for (category of categories; track category) { <option [value]="category">{{ category }}</option> }<option value="Other">Other</option></select></label>
        <label class="file-picker">Choose a file<input type="file" accept=".pdf,.docx,.xlsx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" (change)="chooseFile($event)"><span class="drop-zone"><span class="upload-icon">↑</span><strong>{{ file?.name ?? 'Select a document' }}</strong><small>PDF, DOCX, XLSX · maximum 10 MB</small></span></label>
        @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
        @if (success) { <p class="form-success" role="status">{{ success }}</p> }
        <div class="form-footer"><a class="text-link" routerLink="/documents">Cancel</a><button class="button primary" [disabled]="form.invalid || !file || busy">{{ busy ? 'Uploading…' : 'Upload file' }} <span>→</span></button></div>
      </form>
      <p class="privacy-note"><span>◇</span> Files are stored under a generated name and are only accessible to authorized users.</p>
    </section>
  `
})
export class UploadComponent {
  private readonly fb = inject(FormBuilder);
  private readonly documents = inject(DocumentService);
  private readonly router = inject(Router);
  readonly categories = ['HR', 'Finance', 'Contracts', 'Engineering', 'Legal'];
  readonly form = this.fb.group({ title: ['', [Validators.required, Validators.maxLength(200)]], category: ['', [Validators.required, Validators.maxLength(100)]] });
  file: File | null = null;
  error = '';
  success = '';
  busy = false;

  chooseFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
    this.error = '';
    if (!this.file) return;
    const extension = this.file.name.split('.').pop()?.toLowerCase();
    if (!['pdf', 'docx', 'xlsx'].includes(extension ?? '')) {
      this.file = null;
      this.error = 'Choose a PDF, DOCX, or XLSX file.';
    } else if (this.file.size > 10 * 1024 * 1024) {
      this.file = null;
      this.error = 'Files must be 10 MB or smaller.';
    }
  }

  submit(): void {
    if (this.form.invalid || !this.file || this.busy) return;
    this.busy = true;
    this.error = '';
    const value = this.form.getRawValue();
    this.documents.upload(value.title ?? '', value.category ?? '', this.file).subscribe({
      next: () => { this.success = 'Document uploaded successfully.'; this.busy = false; void this.router.navigate(['/documents']); },
      error: (error: { error?: { message?: string } }) => { this.busy = false; this.error = error.error?.message ?? 'Upload failed. Check your connection and try again.'; }
    });
  }
}
