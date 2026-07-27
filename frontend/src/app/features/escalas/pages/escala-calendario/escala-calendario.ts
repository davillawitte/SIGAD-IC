import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciPageHeaderComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';
import { filter, switchMap } from 'rxjs/operators';

import { openPromptDialog } from '../../../../shared/dialogs/dialog.helpers';
import { EscalasApiService } from '../../services/escalas-api.service';
import type { EscalaDetail } from '../../models/escalas.models';

@Component({
  selector: 'app-escala-calendario',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciPageHeaderComponent,
    PciStackComponent,
  ],
  templateUrl: './escala-calendario.html',
  styleUrl: './escala-calendario.scss',
})
export class EscalaCalendario implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly escala = signal<EscalaDetail | null>(null);
  private listBasePath = '/escalas';

  readonly days = computed(() => {
    const e = this.escala();
    if (!e) return [] as string[];
    const start = new Date(e.dataInicio + 'T00:00:00');
    const end = new Date(e.dataFim + 'T00:00:00');
    const list: string[] = [];
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      list.push(d.toISOString().slice(0, 10));
    }
    return list;
  });

  ngOnInit(): void {
    const escopo = (this.route.snapshot.data['escopo'] as string | undefined) ?? 'setor';
    this.listBasePath = escopo === 'institucional' ? '/escalas-institucionais' : '/escalas';

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Escala inválida.');
      this.loading.set(false);
      return;
    }

    this.api.get(id).subscribe({
      next: (escala) => {
        this.escala.set(escala);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar a escala.');
        this.loading.set(false);
      },
    });
  }

  codeFor(servidorId: string, day: string): string {
    const s = this.escala()?.servidores.find((x) => x.servidorId === servidorId);
    return s?.ocorrencias.find((o) => o.data.slice(0, 10) === day)?.tipoOcorrenciaCodigo ?? '';
  }

  isEditable(): boolean {
    const status = this.escala()?.status;
    return status === 'Rascunho' || status === 'Finalizada';
  }

  onCellClick(servidorId: string, day: string): void {
    const e = this.escala();
    if (!e || (e.status !== 'Rascunho' && e.status !== 'Finalizada')) return;

    const atual = this.codeFor(servidorId, day);
    openPromptDialog(this.dialog, {
      title: 'Código da ocorrência',
      label: `Código do tipo de ocorrência para ${day}`,
      placeholder: atual || 'Ex.: M, T, TL6',
      confirmLabel: 'Salvar',
    })
      .pipe(
        filter((v): v is string => !!v?.trim()),
        switchMap((codigo) => {
          const tipoOcorrenciaCodigo = codigo.trim().toUpperCase();
          return this.api.upsertOcorrencia(e.id, servidorId, {
            data: day,
            tipoOcorrenciaCodigo,
          });
        }),
      )
      .subscribe({
        next: (escala) => this.escala.set(escala),
        error: (err: { error?: { message?: string } }) =>
          this.error.set(err.error?.message ?? 'Não foi possível salvar a ocorrência.'),
      });
  }

  isWeekend(day: string): boolean {
    const d = new Date(day + 'T00:00:00').getDay();
    return d === 0 || d === 6;
  }

  dayLabel(day: string): string {
    const d = new Date(day + 'T00:00:00');
    const letters = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
    return `${d.getDate()}\n${letters[d.getDay()]}`;
  }

  voltar(): void {
    const id = this.escala()?.id;
    if (id) void this.router.navigateByUrl(`${this.listBasePath}/${id}`);
    else void this.router.navigateByUrl(this.listBasePath);
  }

  editar(): void {
    const id = this.escala()?.id;
    if (id) void this.router.navigateByUrl(`/escalas/${id}/editar`);
  }
}
