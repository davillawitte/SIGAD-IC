import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  PciLoginCredentials,
  PciLoginPageComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-login-form',
  imports: [PciLoginPageComponent],
  templateUrl: './login-form.html',
})
export class LoginForm {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  onSubmit(credentials: PciLoginCredentials): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.auth.login(credentials.username, credentials.password).subscribe({
      next: (result) => {
        if (!result.ok) {
          this.errorMessage.set(result.message);
          this.loading.set(false);
          return;
        }

        const target = this.auth.deveAlterarSenha() ? '/trocar-senha' : '/';
        void this.router.navigateByUrl(target);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Não foi possível autenticar.');
        this.loading.set(false);
      },
    });
  }
}
