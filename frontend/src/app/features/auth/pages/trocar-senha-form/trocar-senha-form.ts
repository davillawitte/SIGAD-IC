import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciAuthLayoutComponent,
  PciButtonComponent,
  PciInputComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-trocar-senha-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciAuthLayoutComponent,
    PciButtonComponent,
    PciInputComponent,
    PciStackComponent,
  ],
  templateUrl: './trocar-senha-form.html',
  styleUrl: './trocar-senha-form.scss',
})
export class TrocarSenhaForm {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    senhaAtual: ['', Validators.required],
    novaSenha: ['', [Validators.required, Validators.minLength(8)]],
    confirmarSenha: ['', Validators.required],
  });

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha todos os campos. A nova senha deve ter ao menos 8 caracteres.');
      return;
    }

    const { senhaAtual, novaSenha, confirmarSenha } = this.form.getRawValue();
    if (novaSenha !== confirmarSenha) {
      this.error.set('A confirmação não confere com a nova senha.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.auth.alterarSenha(senhaAtual, novaSenha).subscribe({
      next: (result) => {
        if (!result.ok) {
          this.error.set(result.message);
          this.loading.set(false);
          return;
        }

        void this.router.navigateByUrl('/');
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível alterar a senha.');
        this.loading.set(false);
      },
    });
  }
}
