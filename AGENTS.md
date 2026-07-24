# SIGAD-IC — orientações permanentes do agente

## Design system first (PCI)

Sempre use `@davillawitte/pci-design-system` para UI neste projeto.

### Obrigatório

- Preferir componentes da lib: `pci-button`, `pci-input`, `pci-select`, `pci-datepicker`, `pci-checkbox`, `pci-alert`, `pci-badge`, `pci-tabs`, `pci-stepper`, `pci-toast` / `PciToastService`, `pci-feedback-modal` / `PciFeedbackModalService`, `pci-page-header`, `pci-form-page`, `pci-list-page`, `pci-stack`, ícones nos botões, etc.
- Antes de montar UI “na mão” (HTML / `mat-*` cru / CSS custom de form control), verificar se a lib já oferece o equivalente.
- Selects → `pci-select` (não `mat-select` solto com wrapper `pci-field`).
- Feedback de sucesso/erro de ação → toast ou feedback modal da lib (não `window.alert` / texto solto).
- Stepper / tabs / badges / botões com ícone → componentes da lib.

### Exceção

Só use Angular Material cru, HTML custom ou CSS próprio quando **não existir** equivalente no design system (ou o componente da lib for insuficiente de forma comprovada).

### Ao implementar

1. Consultar exports do pacote `@davillawitte/pci-design-system`.
2. Seguir o mesmo padrão visual das telas já alinhadas à lib.
3. Não reinventar controles que a lib já oferece.
