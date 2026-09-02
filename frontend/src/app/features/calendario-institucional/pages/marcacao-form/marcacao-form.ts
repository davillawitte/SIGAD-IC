import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectComponent,
  PciTextareaComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { CALENDARIO_ROUTE_PAGES } from '../../calendario-institucional.routes.meta';
import { CalendarioApiService } from '../../services/calendario-api.service';
import type { TipoMarcacaoCalendario } from '../../services/calendario-api.service';
import { TIPO_MARCACAO_OPTIONS, isEvento } from '../../models/calendario.models';

@Component({
  selector: 'app-marcacao-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectComponent,
    PciTextareaComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './marcacao-form.html',
})
export class MarcacaoForm implements OnInit {
  private readonly api = inject(CalendarioApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = CALENDARIO_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPath = signal('/calendario-institucional/:ano/marcacoes/novo');
  readonly tipoOptions: PciSelectOption[] = TIPO_MARCACAO_OPTIONS.map((t) => ({
    label: t.label,
    value: t.value,
  }));

  readonly form = this.fb.nonNullable.group({
    tipo: ['PontoFacultativo' as TipoMarcacaoCalendario, Validators.required],
    data: ['', Validators.required],
    dataFim: [''],
    nome: ['', Validators.required],
    descricao: [''],
    local: [''],
    publicoAlvo: [''],
  });

  private ano = 0;
  private calendarioAnoId: string | null = null;
  private editId: string | null = null;

  ngOnInit(): void {
    this.ano = Number(this.route.snapshot.paramMap.get('ano'));
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(
      this.editId
        ? '/calendario-institucional/:ano/marcacoes/:id/editar'
        : '/calendario-institucional/:ano/marcacoes/novo',
    );

    this.api.obterAno(this.ano).subscribe({
      next: (calendario) => {
        this.calendarioAnoId = calendario.id;
        if (this.editId) {
          const marcacao = calendario.marcacoes.find((m) => m.id === this.editId);
          if (!marcacao) {
            this.error.set('Marcação não encontrada.');
            this.form.disable();
            return;
          }
          this.form.patchValue({
            tipo: marcacao.tipo,
            data: marcacao.data.slice(0, 10),
            dataFim: marcacao.dataFim ? marcacao.dataFim.slice(0, 10) : '',
            nome: marcacao.nome,
            descricao: marcacao.descricao ?? '',
            local: marcacao.local ?? '',
            publicoAlvo: marcacao.publicoAlvo ?? '',
          });
        }
      },
      error: () => this.error.set('Não foi possível carregar o calendário deste ano.'),
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }

    const value = this.form.getRawValue();
    const evento = isEvento(value.tipo);
    const payload = {
      tipo: value.tipo,
      data: value.data,
      dataFim: value.dataFim || null,
      nome: value.nome.trim(),
      descricao: evento ? value.descricao.trim() || null : null,
      local: evento ? value.local.trim() || null : null,
      publicoAlvo: evento ? value.publicoAlvo.trim() || null : null,
    };

    this.saving.set(true);
    this.error.set(null);

    if (!this.isEdit()) {
      if (!this.calendarioAnoId) {
        this.error.set('Calendário do ano não encontrado.');
        this.saving.set(false);
        return;
      }
      this.api.adicionarMarcacao({ calendarioAnoId: this.calendarioAnoId, ...payload }).subscribe({
        next: () => void this.router.navigateByUrl(`/calendario-institucional/${this.ano}`),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível salvar.');
          this.saving.set(false);
        },
      });
      return;
    }

    this.api.atualizarMarcacao(this.editId!, payload).subscribe({
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

  mostrarCamposEvento(): boolean {
    return isEvento(this.form.controls.tipo.value);
  }
}
