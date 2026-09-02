import { Component, Input } from '@angular/core';
import { Key } from 'lucide';

export type AppIconName = 'key';
export type AppIconSize = 'xs' | 'sm' | 'md' | 'lg';

type IconNode = readonly [tag: string, attrs: Record<string, string>][];

/** Ícones que o design system (`@davillawitte/pci-design-system`) ainda não tem — ver
 * `docs/design-system-pendencias.md` para a lista completa e a especificação de cada um pra
 * portar pro pacote quando existir suporte lá. Réplica fiel de `PciIconComponent`: mesmo SVG
 * (viewBox 24x24, stroke arredondado), mesmas classes/tamanhos de host, mesmo `strokeWidth`
 * padrão — só a fonte do glifo (nós crus do pacote `lucide`, a mesma lib que o design system usa
 * por baixo) e o registro (fechado a este punhado de ícones) são diferentes. */
const APP_ICON_REGISTRY: Record<AppIconName, IconNode> = {
  key: Key as unknown as IconNode,
};

@Component({
  selector: 'app-icon',
  templateUrl: './app-icon.html',
  styleUrl: './app-icon.scss',
  host: {
    class: 'app-icon',
    '[class]': 'hostClasses',
  },
})
export class AppIconComponent {
  @Input({ required: true }) name!: AppIconName;
  @Input() size: AppIconSize = 'md';
  @Input() strokeWidth = 1.75;
  @Input() label?: string;

  get hostClasses(): string {
    return `app-icon app-icon--${this.size}`;
  }

  get iconNodes(): IconNode {
    return APP_ICON_REGISTRY[this.name];
  }

  tag(element: IconNode[number]): string {
    return element[0];
  }

  attr(element: IconNode[number], key: string): string | null {
    return element[1][key] ?? null;
  }
}
