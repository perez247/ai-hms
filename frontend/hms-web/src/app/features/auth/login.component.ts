import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';

/** Staff login with demo-role shortcuts; successful login lands on the role home route. */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly showPassword = signal(false);
  readonly form = this.fb.nonNullable.group({
    email: ['finance@clinic.local', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required, Validators.minLength(10)]],
  });

  readonly demoAccounts = [
    ['admin@clinic.local', 'Admin'],
    ['receptionist@clinic.local', 'Receptionist'],
    ['nurse@clinic.local', 'Nurse'],
    ['doctor@clinic.local', 'Doctor'],
    ['pharmacy@clinic.local', 'Pharmacy'],
    ['finance@clinic.local', 'Finance'],
    ['hmo@clinic.local', 'HMO Office'],
    ['hr@clinic.local', 'HR Office'],
  ];

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          void this.router.navigateByUrl(returnUrl ?? this.auth.homeForRole(response.user.role));
        },
        error: (err) => {
          void err;
        },
      });
  }

  useDemo(email: string): void {
    this.form.patchValue({ email, password: 'ChangeMe123!' });
  }
}
