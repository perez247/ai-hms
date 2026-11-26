import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { PromptService } from '../../core/prompt.service';
import { ToastService } from '../../core/toast.service';
import { StaffRole, UserRecord } from '../../core/models';

/** HR account creation, disablement and CSV export. */
@Component({
  selector: 'app-hr-users',
  imports: [ReactiveFormsModule, CurrencyPipe, RouterLink],
  templateUrl: './hr-users.component.html',
})
export class HrUsersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly prompts = inject(PromptService);
  readonly users = signal<UserRecord[]>([]);
  readonly notice = signal<string | null>(null);
  readonly showCreate = signal(false);
  readonly roles: StaffRole[] = ['Receptionist', 'Nurse', 'Doctor', 'Pharmacy', 'Finance', 'HmoOffice', 'HrOffice', 'Lab', 'Admin'];
  readonly form = this.fb.nonNullable.group({
    fullName: ['', Validators.required], email: ['', [Validators.required, Validators.email]],
    password: ['ChangeMe123!', [Validators.required, Validators.minLength(10)]],
    staffRole: ['Receptionist' as StaffRole, Validators.required], employeeNumber: [''],
    phone: [''], salaryAmount: [0], bankName: [''], bankAccountName: [''], bankAccountNumber: [''],
  });
  activeFilter: boolean | null = true;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.hr.users(this.activeFilter ?? undefined).subscribe({
      next: (page) => this.users.set(page.items),
      error: (err) => void err,
    });
  }

  create(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.api.hr.create({ ...value, salaryAmount: Number(value.salaryAmount) || null, employeeNumber: value.employeeNumber || null }).subscribe({
      next: () => { this.notice.set('User account created.'); this.showCreate.set(false); this.form.reset({ staffRole: 'Receptionist', password: 'ChangeMe123!' }); this.load(); },
      error: (err) => void err,
    });
  }

  async disable(user: UserRecord): Promise<void> {
    const values = await this.prompts.open({
      title: `Disable ${user.fullName}?`,
      description: 'The account will no longer be able to sign in. Provide an audit reason.',
      fields: [{ key: 'reason', label: 'Reason', placeholder: 'Reason for disabling this account' }],
      confirmLabel: 'Disable account',
    });
    if (!values) return;
    const reason = (values['reason'] ?? '').trim();
    if (!reason) return;
    this.api.hr.disable(user.id, reason).subscribe({
      next: () => { this.notice.set(`${user.fullName} disabled.`); this.load(); },
      error: (err) => void err,
    });
  }

  export(): void { this.api.hr.export(this.activeFilter ?? undefined); }
}
