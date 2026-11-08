import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../core/auth.service';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="auth-layout">
      <section class="auth-art">
        <a class="brand light" routerLink="/"><span class="brand-mark">T</span><span>trustvault</span></a>
        <div class="art-copy"><span class="eyebrow">A CALMER WAY TO KEEP CONTROL</span><h1>Your work,<br>under lock<br>and key.</h1><p>One secure place for the documents that move your business forward.</p></div>
        <div class="art-foot"><span>PRIVATE BY DESIGN</span><span>01 / TRUSTED ACCESS</span></div>
      </section>
      <section class="auth-panel">
        <div class="auth-form-wrap">
          <span class="eyebrow">{{ isRegister ? 'CREATE YOUR ACCOUNT' : 'WELCOME BACK' }}</span>
          <h2>{{ isRegister ? 'Start your workspace' : 'Sign in to TrustVault' }}</h2>
          <p class="muted">{{ isRegister ? 'Your secure document workspace starts here.' : 'Enter your credentials to continue.' }}</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            @if (isRegister) {
              <label>Full name<input formControlName="name" autocomplete="name" placeholder="Your name"></label>
            }
            <label>Email address<input formControlName="email" autocomplete="email" type="email" placeholder="you@company.com"></label>
            <label>Password<input formControlName="password" [autocomplete]="isRegister ? 'new-password' : 'current-password'" type="password" placeholder="At least 8 characters"></label>
            @if (isRegister) {
              <label>Workspace role<select formControlName="role"><option value="Owner">Owner</option><option value="Viewer">Viewer</option></select></label>
            }
            @if (error) { <p class="form-error" role="alert">{{ error }}</p> }
            <button class="button primary full" [disabled]="form.invalid || busy">{{ busy ? 'Please wait…' : (isRegister ? 'Create account' : 'Sign in') }} <span>→</span></button>
          </form>
          <p class="auth-switch">{{ isRegister ? 'Already have an account?' : 'New to TrustVault?' }} <a [routerLink]="isRegister ? '/login' : '/register'">{{ isRegister ? 'Sign in' : 'Create an account' }}</a></p>
          <p class="auth-note">Admin access is provisioned by your system administrator.</p>
        </div>
      </section>
    </main>
  `
})
export class AuthPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly isRegister = inject(ActivatedRoute).snapshot.data['mode'] === 'register';
  readonly form = this.fb.group({
    name: ['', this.isRegister ? [Validators.required, Validators.maxLength(100)] : []],
    email: ['', [Validators.required, Validators.email]],
    password: ['', this.isRegister ? [Validators.required, Validators.minLength(8)] : [Validators.required]],
    role: ['Owner']
  });
  error = '';
  busy = false;

  submit(): void {
    if (this.form.invalid || this.busy) return;
    this.error = '';
    this.busy = true;
    const value = this.form.getRawValue();
    if (this.isRegister) {
      this.auth.register(value.name ?? '', value.email ?? '', value.password ?? '', value.role === 'Viewer' ? 'Viewer' : 'Owner').subscribe({
        next: () => { this.busy = false; void this.router.navigate(['/login']); },
        error: (error: { error?: { message?: string } }) => { this.busy = false; this.error = error.error?.message ?? 'Could not create your account.'; }
      });
    } else {
      this.auth.login(value.email ?? '', value.password ?? '').subscribe({
        next: () => { this.busy = false; void this.router.navigate(['/']); },
        error: (error: { error?: { message?: string } }) => { this.busy = false; this.error = error.error?.message ?? 'Could not sign in. Check your connection and try again.'; }
      });
    }
  }
}
