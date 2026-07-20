import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `<section class="home" aria-label="Página inicial"></section>`,
  styles: `
    .home {
      min-height: 12rem;
    }
  `,
})
export class HomeComponent {}
