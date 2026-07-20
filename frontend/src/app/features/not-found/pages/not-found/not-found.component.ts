import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <section class="not-found">
      <h1>404</h1>
      <p>Página não encontrada.</p>
      <a routerLink="/">Voltar ao início</a>
    </section>
  `,
  styles: `
    .not-found {
      text-align: center;
      padding: 4rem 1rem;
    }

    h1 {
      font-size: 4rem;
      margin: 0;
      color: #4361ee;
    }

    p {
      color: #6b7280;
      margin: 0.5rem 0 1.5rem;
    }

    a {
      color: #4361ee;
      text-decoration: none;
      font-weight: 500;
    }

    a:hover {
      text-decoration: underline;
    }
  `,
})
export class NotFoundComponent {}
