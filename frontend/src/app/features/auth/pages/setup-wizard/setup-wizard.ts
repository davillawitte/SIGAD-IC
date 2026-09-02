import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciAuthLayoutComponent,
  PciButtonComponent,
  PciInputComponent,
  PciStackComponent,
  PciStepComponent,
  PciStepperComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { SetupApiService } from '../../../../core/auth/setup-api.service';

type WizardStep = 1 | 2 | 3;

@Component({
  selector: 'app-setup-wizard',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciAuthLayoutComponent,
    PciButtonComponent,
    PciInputComponent,
    PciStackComponent,
    PciStepComponent,
    PciStepperComponent,
  ],
  templateUrl: './setup-wizard.html',
  styleUrl: './setup-wizard.scss',
})
export class SetupWizard {
  private readonly api = inject(SetupApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly step = signal<WizardStep>(1);
  readonly stepperIndex = computed(() => this.step() - 1);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly tokenForm = this.fb.nonNullable.group({
    token: ['', Validators.required],
  });

  readonly dadosForm = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    cpf: ['', Validators.required],
    matricula: ['', Validators.required],
    email: [''],
    dataNascimento: ['', Validators.required],
  });

  readonly senhaForm = this.fb.nonNullable.group({
    senha: ['', [Validators.required, Validators.minLength(8)]],
    confirmarSenha: ['', Validators.required],
  });

  avancarToken(): void {
    this.tokenForm.markAllAsTouched();
    if (this.tokenForm.invalid) {
      this.error.set('Informe o token de provisionamento.');
      return;
    }

    this.error.set(null);
    this.step.set(2);
  }

  avancarDados(): void {
    this.dadosForm.markAllAsTouched();
    if (this.dadosForm.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }

    this.error.set(null);
    this.step.set(3);
  }

  voltar(): void {
    this.error.set(null);
    this.step.set(Math.max(1, this.step() - 1) as WizardStep);
  }

  concluir(): void {
    this.senhaForm.markAllAsTouched();
    if (this.senhaForm.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }

    const { senha, confirmarSenha } = this.senhaForm.getRawValue();
    if (senha !== confirmarSenha) {
      this.error.set('A confirmação não confere com a senha.');
      return;
    }

    const dados = this.dadosForm.getRawValue();
    this.saving.set(true);
    this.error.set(null);

    this.api
      .complete({
        token: this.tokenForm.getRawValue().token,
        cpf: dados.cpf,
        nome: dados.nome,
        matricula: dados.matricula,
        email: dados.email.trim() || null,
        dataNascimento: dados.dataNascimento,
        senha,
      })
      .subscribe({
        next: (response) => {
          this.auth.persistSession(response);
          void this.router.navigateByUrl('/trocar-senha');
        },
        error: (err: { error?: { message?: string; errors?: string[] } }) => {
          this.error.set(
            err.error?.message ?? err.error?.errors?.[0] ?? 'Não foi possível concluir o provisionamento.',
          );
          this.saving.set(false);
        },
      });
  }
}
