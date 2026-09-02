import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PciAlertComponent, PciFormPageComponent, PciSelectComponent } from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { CALENDARIO_ROUTE_PAGES } from '../../calendario-institucional.routes.meta';
import { CalendarioApiService } from '../../services/calendario-api.service';

const OPCOES_DISPONIVEIS = 5;

@Component({
  selector: 'app-calendario-novo',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciSelectComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './calendario-novo.html',
})
export class CalendarioNovo implements OnInit {
  private readonly api = inject(CalendarioApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = CALENDARIO_ROUTE_PAGES;
  readonly currentPath = '/calendario-institucional/novo';
  readonly saving = signal(false);
  readonly loadingAnos = signal(true);
  readonly error = signal<string | null>(null);
  readonly anoOptions = signal<PciSelectOption[]>([]);

  readonly form = this.fb.nonNullable.group({
    ano: ['', Validators.required],
  });

  ngOnInit(): void {
    this.form.controls.ano.disable();
    this.api.listarAnos().subscribe({
      next: (anos) => {
        const existentes = new Set(anos.map((a) => a.ano));
        const options: PciSelectOption[] = [];
        const anoAtual = new Date().getFullYear();
        let candidato = anoAtual - 1;
        while (options.length < OPCOES_DISPONIVEIS) {
          if (!existentes.has(candidato)) {
            options.push({ label: String(candidato), value: String(candidato) });
          }
          candidato++;
        }
        this.anoOptions.set(options);
        this.form.controls.ano.enable();
        this.form.patchValue({ ano: options[0]?.value ?? '' });
        this.loadingAnos.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar os anos já cadastrados.');
        this.loadingAnos.set(false);
      },
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Selecione o ano do calendário.');
      return;
    }

    const ano = Number(this.form.getRawValue().ano);
    if (!ano) {
      this.error.set('Selecione o ano do calendário.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.api.gerarAno(ano).subscribe({
      next: (calendario) =>
        void this.router.navigateByUrl(`/calendario-institucional/${calendario.ano}`),
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível gerar o calendário.');
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/calendario-institucional');
  }
}
