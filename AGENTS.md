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

## Organização de features (frontend)

Cada feature em `frontend/src/app/features/<feature>/`:

```text
pages/
  <nome>-list/     # listagem/consulta → .ts + .html + .scss
  <nome>-form/     # cadastro/edição (wizard em etapas e cópia também são form)
  <nome>-detail/   # só visualização, quando não for list nem form
components/        # pedaços reutilizáveis (pasta por componente)
services/
models/
<feature>.routes.ts
```

- Lista → `*-list/`. Cadastra/edita → `*-form/`.
- Sempre `templateUrl` / `styleUrl` externos (proibido HTML/CSS inline no `.ts`).
- Arquivos **sem** sufixo `.component` (`escala-list.ts`, não `escala-list.component.ts`).
- Detalhe da regra: `.cursor/rules/frontend-feature-layout.mdc`.

### Ao implementar uma funcionalidade:
1. Verifique se o projeto compila sem erros.
2. Execute os testes automatizados existentes.
3. Verifique se as APIs afetadas continuam funcionando.
4. Verifique se as funcionalidades relacionadas à alteração continuam funcionando.
5. Não altere funcionalidades existentes sem necessidade.
6. Se uma alteração puder causar impacto em outra área do sistema,
   faça uma análise de regressão.
7. Informe no final:
   - O que foi alterado;
   - Quais testes foram executados;
   - Quais áreas foram verificadas;
   - Se existe algum risco de regressão.
