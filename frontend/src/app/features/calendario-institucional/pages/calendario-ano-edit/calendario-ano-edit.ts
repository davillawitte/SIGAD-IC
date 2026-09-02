import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PciAlertComponent, PciFormPageComponent, PciTextareaComponent } from '@davillawitte/pci-design-system';

import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { CALENDARIO_ROUTE_PAGES } from '../../calendario-institucional.routes.meta';
import { CalendarioApiService } from '../../services/calendario-api.service';

@Component({
  selector: 'app-calendario-ano-edit',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciTextareaComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './calendario-ano-edit.html',
})
export class CalendarioAnoEdit implements OnInit {
  private readonly api = inject(CalendarioApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = CALENDARIO_ROUTE_PAGES;
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPath = signal('/calendario-institucional/:ano/editar');

  readonly form = this.fb.nonNullable.group({
    observacao: [''],
  });

  private ano = 0;
  private calendarioAnoId: string | null = null;

  ngOnInit(): void {
    this.ano = Number(this.route.snapshot.paramMap.get('ano'));
    this.currentPath.set(`/calendario-institucional/${this.ano}/editar`);

    this.api.obterAno(this.ano).subscribe({
      next: (calendario) => {
        this.calendarioAnoId = calendario.id;
        this.form.patchValue({ observacao: calendario.observacao ?? '' });
      },
      error: () => this.error.set('Não foi possível carregar o calendário deste ano.'),
    });
  }

  save(): void {
    if (!this.calendarioAnoId) {
      this.error.set('Calendário do ano não encontrado.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const observacao = this.form.getRawValue().observacao.trim() || null;
    this.api.atualizarAno(this.calendarioAnoId, observacao).subscribe({
      next: () => void this.router.navigateByUrl(`/calendario-institucional/${this.ano}`),
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível salvar.');
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    void this.router.navigateByUrl(`/calendario-institucional/${this.ano}`);
  }
}
