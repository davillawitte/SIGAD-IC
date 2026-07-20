import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciCardComponent,
  PciCardContentComponent,
  PciCardDescriptionComponent,
  PciCardHeaderComponent,
  PciCardTitleComponent,
  PciSpinnerComponent,
} from '@davillawitte/pci-design-system';

import { HealthService } from '../../../../core/services/health.service';
import { HealthStatus } from '../../../../core/models/health-status.model';

@Component({
  selector: 'app-home',
  imports: [
    CommonModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciCardComponent,
    PciCardContentComponent,
    PciCardDescriptionComponent,
    PciCardHeaderComponent,
    PciCardTitleComponent,
    PciSpinnerComponent,
  ],
  template: `
    <section class="home">
      <pci-alert
        variant="info"
        title="Gestão IC"
        message="Primeiro sistema da plataforma PCI/RN — template base em Angular 20, .NET 10 e PostgreSQL."
      />

      <pci-card class="home__card">
        <pci-card-header>
          <pci-card-title>Status da API</pci-card-title>
          <pci-card-description>Conexão com o backend via GET /health</pci-card-description>
        </pci-card-header>

        <pci-card-content>
          @if (loading()) {
            <div class="home__loading">
              <pci-spinner [size]="24" />
              <span>Verificando conexão...</span>
            </div>
          } @else if (error()) {
            <pci-alert variant="error" title="Erro de conexão" [message]="error()!" />
          } @else if (health()) {
            <div class="home__status-row">
              <span>Status geral</span>
              <pci-badge [variant]="badgeVariant()" [dot]="true">{{ health()?.status }}</pci-badge>
            </div>

            <dl class="home__details">
              <div>
                <dt>Duração</dt>
                <dd>{{ health()?.totalDuration }}</dd>
              </div>
              @for (entry of healthEntries(); track entry.key) {
                <div>
                  <dt>{{ entry.key }}</dt>
                  <dd>{{ entry.value.status }}</dd>
                </div>
              }
            </dl>
          }
        </pci-card-content>
      </pci-card>
    </section>
  `,
  styles: `
    .home {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .home__card {
      display: block;
    }

    .home__loading {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      color: var(--pci-color-text-secondary, #6b7280);
    }

    .home__status-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .home__details {
      display: grid;
      gap: 0.5rem;
      margin: 0;
    }

    .home__details div {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      border-top: 1px solid var(--pci-color-border, #e5e7eb);
    }

    dt {
      color: var(--pci-color-text-secondary, #6b7280);
    }

    dd {
      margin: 0;
      font-weight: 500;
    }
  `,
})
export class HomeComponent {
  private readonly healthService = inject(HealthService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly health = signal<HealthStatus | null>(null);

  readonly healthEntries = signal<
    { key: string; value: NonNullable<HealthStatus['entries']>[string] }[]
  >([]);

  readonly badgeVariant = computed(() => {
    const status = this.health()?.status;
    if (status === 'Healthy') return 'success' as const;
    if (status === 'Degraded') return 'warning' as const;
    return 'error' as const;
  });

  constructor() {
    this.healthService.checkHealth().subscribe({
      next: (status) => {
        this.health.set(status);
        this.healthEntries.set(
          Object.entries(status.entries ?? {}).map(([key, value]) => ({ key, value })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível conectar à API. Verifique se o backend está em execução.');
        this.loading.set(false);
      },
    });
  }
}
