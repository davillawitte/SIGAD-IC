import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet],
  template: `
    <div class="layout">
      <header class="layout__header">
        <div class="layout__brand">
          <span class="layout__logo">PCI</span>
          <div>
            <span class="layout__title">Gestão IC</span>
            <span class="layout__subtitle">Instituto de Criminalística — RN</span>
          </div>
        </div>
      </header>
      <main class="layout__content">
        <router-outlet />
      </main>
      <footer class="layout__footer">
        <small>Polícia Científica do RN</small>
      </footer>
    </div>
  `,
  styles: `
    .layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .layout__header {
      padding: 1rem 2rem;
      background: var(--pci-color-navy, #0e1e45);
      color: var(--pci-color-gold, #c8a040);
    }

    .layout__brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .layout__logo {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 0.5rem;
      border: 2px solid var(--pci-color-gold, #c8a040);
      font-weight: 700;
      font-size: 0.75rem;
      color: var(--pci-color-gold, #c8a040);
    }

    .layout__title {
      display: block;
      font-weight: 600;
      color: #fff;
      letter-spacing: 0.02em;
    }

    .layout__subtitle {
      display: block;
      font-size: 0.75rem;
      color: rgb(255 255 255 / 70%);
    }

    .layout__content {
      flex: 1;
      padding: 2rem;
      max-width: 960px;
      width: 100%;
      margin: 0 auto;
    }

    .layout__footer {
      padding: 1rem 2rem;
      text-align: center;
      color: var(--pci-color-text-secondary, #6b7280);
      border-top: 1px solid var(--pci-color-border, #e5e7eb);
      background: #fff;
    }
  `,
})
export class MainLayoutComponent {}
