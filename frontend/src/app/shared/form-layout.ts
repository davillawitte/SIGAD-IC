import { ChangeDetectionStrategy, Component, Directive, HostBinding, Input } from '@angular/core';

/** Seção de formulário com fieldset + legend (nome do grupo em cima da borda). */
@Component({
  selector: 'app-form-section',
  standalone: true,
  templateUrl: './form-section.component.html',
  styleUrl: './form-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppFormSectionComponent {
  @Input() title = '';
  @Input() description = '';
}

export type AppFormColSpan = 3 | 4 | 6 | 8 | 12;

/** Equivalente local a `[pciFormCol]`. */
@Directive({
  selector: '[appFormCol]',
  standalone: true,
})
export class AppFormColDirective {
  @Input() appFormCol: AppFormColSpan = 4;

  @HostBinding('class')
  get hostClass(): string {
    return `pci-form-col pci-form-col--${this.appFormCol}`;
  }

  @HostBinding('style.grid-column')
  get gridColumn(): string {
    if (this.appFormCol === 12) {
      return '1 / -1';
    }
    if (this.appFormCol === 8 || this.appFormCol === 6) {
      return 'span 2';
    }
    return 'span 1';
  }
}
