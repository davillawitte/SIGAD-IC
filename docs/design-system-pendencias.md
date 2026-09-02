# Pendências do design system

Lista de coisas implementadas localmente neste repositório porque
`@davillawitte/pci-design-system` (pacote externo publicado, não faz parte deste repositório)
ainda não oferece suporte — pra portar pro pacote quando ele ganhar o recurso correspondente.

## Ícone "key" (chave)

- **Por quê**: o design system só tem `lock`/`unlock` em `PciIconName`; não existe `key`.
- **Onde teria uso**: ação "Resetar senha" na listagem de usuários
  (`frontend/src/app/features/admin/pages/usuario-list/usuario-list.ts`), hoje com `icon: 'lock'`.
- **Por que não foi aplicado ainda**: essa ação é um `PciRowAction`, renderizado inteiramente
  dentro do `pci-data-table` do próprio design system (via `pci-icon`/`pci-icon-button`
  internos) — não há como injetar um ícone fora do union `PciIconName` nesse ponto sem tocar o
  pacote. A única extensão pública de célula customizada
  (`PciTableCellDirective`/`[pciTableCell]`) só sobrescreve colunas de dados, e só funciona em
  telas que usam `pci-data-table` diretamente — nenhuma tela deste app usa (todas usam
  `pci-list-page`, que não repassa conteúdo projetado pra dentro da tabela interna). Decisão:
  manter `lock` por enquanto; revisitar quando o design system ganhar `key` nativamente.
- **O que já existe pronto localmente**: `frontend/src/app/shared/icons/app-icon.ts`
  (`AppIconComponent`, seletor `app-icon`) — réplica fiel de `PciIconComponent` (mesmo SVG
  `viewBox="0 0 24 24"`, `stroke-width` padrão `1.75`, `stroke-linecap`/`stroke-linejoin="round"`,
  mesmas classes/tamanhos de host `xs=14px/sm=16px/md=20px/lg=24px`), com um registro local
  pequeno (`APP_ICON_REGISTRY`) alimentado pelo nó cru `Key` do pacote `lucide` (a mesma lib que
  o design system usa por baixo — `lucide` já é dependência transitiva dele, adicionada aqui
  como dependência direta em `frontend/package.json`). Pronto pra uso em qualquer lugar onde o
  template seja controlado diretamente por este app (fora do `pci-data-table`).
- **Spec pra portar**: adicionar `'key'` a `PciIconName` e ao `PCI_ICON_REGISTRY`
  (`icon('key', Key)`, mesmo padrão dos ícones existentes) no pacote do design system.

## Tamanho de fonte de `pci-alert__message` dentro de modais

- **Por quê**: `PciAlertComponent` (usado via `<pci-alert variant="error|success" ...>`) renderiza
  a mensagem com `.pci-alert__message { font-size: var(--pci-font-size-base) }` (14px) — pequeno
  demais dentro dos modais desta aplicação (`ConfirmDialog`, `PromptDialog`,
  `AfastamentoDialog`, `ServidorDialog`, `UsuarioDialog`), inconsistente com o resto do texto do
  modal depois que `.pci-app-dialog__message` (nosso, 16px) foi aumentado.
- **Onde foi contornado**: `frontend/src/styles.scss`, regra
  `.pci-app-dialog .pci-alert__message { font-size: var(--pci-font-size-md) !important; }` —
  escopada só dentro de `.pci-app-dialog` (não afeta `pci-alert` usado fora de modais, que
  continua 14px). O `!important` é necessário porque a regra original vem injetada pelo pacote
  compilado (sem garantia de vencer por especificidade/ordem de carregamento).
- **Spec pra portar**: `PciAlertComponent` poderia expor um `size` (ou herdar de um contexto/
  token maior) pra telas que precisem de texto mais legível, em vez de fixar `--pci-font-size-base`
  internamente sem chance de ajuste externo sem `!important`.

## `PciInputComponent` não sincroniza autopreenchimento do navegador

- **Por quê**: o gerenciador de senhas do navegador escreve direto no `<input>` nativo (dentro do
  `pci-input`) sem disparar o evento `input` que `PciInputComponent.onInput()` escuta — o
  `FormControl` correspondente fica com valor vazio mesmo com o campo visivelmente preenchido
  (dots do navegador), então `Validators.required` acusa erro incorretamente ("Campo
  obrigatório." aparece com o campo cheio).
- **Onde foi contornado**: `frontend/src/app/features/auth/pages/trocar-senha-form/`
  (`trocar-senha-form.scss` + `.ts`) — truque `:-webkit-autofill` + `animationstart`: uma
  animação CSS dispara quando o Chrome autopreenche (`:host ::ng-deep input:-webkit-autofill`,
  `::ng-deep` necessário pra alcançar o `<input>` nativo dentro do `pci-input`), e
  `TrocarSenhaForm.onAutofillAnimationStart()` escuta esse evento e dispara um `input` sintético
  no mesmo elemento — o próprio `(input)="onInput($event)"` do `pci-input` sincroniza o
  `FormControl` a partir daí. Só aplicado nesta tela; qualquer outro `pci-input type="password"`
  no app pode ter o mesmo problema.
- **Spec pra portar**: o certo seria `PciInputComponent` já tratar isso internamente — escutar
  `animationstart`/`:-webkit-autofill` (ou usar a API `chrome.autofill`/`compositionupdate`
  conforme aplicável) no próprio `ControlValueAccessor` e chamar `onChange`/`onTouched`
  automaticamente, sem exigir esse workaround em cada tela que usa o componente.

## `PciFilterFieldType` não tem tipo `'number'`

- **Por quê**: `PciFilterField` (usado em `pci-list-page` pra declarar filtros) só aceita
  `type: 'text' | 'select' | 'date' | 'dateRange'` — não existe `'number'`, nem `pattern`/
  `inputMode` no tipo pra restringir o teclado num campo declarativo renderizado por dentro do
  componente da lib.
- **Onde teria uso**: filtro "Ano" na listagem de escalas
  (`frontend/src/app/features/escalas/pages/escala-list/escala-list.ts`, compartilhado entre a
  visão de setor e a institucional), hoje `type: 'text'` — o usuário consegue digitar letras
  numa caixa que só faz sentido como número.
- **Por que não foi aplicado ainda**: sem um tipo `'number'` (ou `pattern`) exposto pelo
  `PciFilterField`, não há como restringir a entrada de dentro deste app — o campo é renderizado
  inteiramente pelo componente da lib.
- **Já é seguro assim**: `reload()` (`escala-list.ts`, `Number(filters['ano'] || '')` +
  `Number.isFinite(...)`) já descarta texto não numérico antes de mandar pro backend — o filtro
  é apenas ignorado nesse caso, não quebra a busca. É só uma limitação de experiência de
  digitação, não um bug funcional.
- **Spec pra portar**: `PciFilterFieldType` ganhar `'number'` (ou `PciFilterField` expor
  `pattern`/`inputMode`) pro pacote.

## Visual de modal (selo + título + risco dourado + rodapé) sem componente na lib

- **Por quê**: o padrão visual acordado pros modais do app (referência:
  `docs/models images/modal choices.png` e `trocar senha.png`) tem selo circular com ícone em tom
  dourado/âmbar, título grande centralizado, risco dourado curto abaixo do título, caixa de
  destaque azul clara pra mensagens de confirmação e "X" de fechar no canto — nenhum desses
  elementos existe em `PciCard`/`MatDialog`/`PciAlert` da lib.
- **Onde foi implementado**: `frontend/src/app/shared/dialogs/dialog-header/dialog-header.ts`
  (`<app-dialog-header>`, `AppDialogHeaderComponent`) — selo + título + risco dourado + "X"
  (`closed` output; `[closable]="false"` esconde o "X" quando não faz sentido fechar, ex.:
  `trocar-senha-form.html`, tela cheia sem modal por trás). Usado em todos os diálogos custom do
  app: `ConfirmDialog`, `PromptDialog`, `ExportCsvDialog`, `ConflitosDialog` (novo, conflitos de
  publicação de escala), `UsuarioDialog`, `ServidorDialog`, `AfastamentoDialog`, e na página
  `TrocarSenhaForm`. Estilos complementares (`.pci-app-dialog__info-box`, `.pci-app-dialog__link`,
  divisor antes do rodapé) em `frontend/src/styles.scss`, seção "Dialogs no padrão visual PCI".
- **Spec pra portar**: um componente `PciModalHeader`/variante de `PciCard` com selo circular
  (ícone + cor de fundo configuráveis), título centralizado com risco dourado, e slot de "X" de
  fechar, além de uma variante de `PciAlert`/card pra caixa de destaque informativa dentro de
  modal (o que hoje é `.pci-app-dialog__info-box`).

## `PciInputComponent` não tem prefixo de ícone nem alternância de mostrar/ocultar senha

- **Por quê**: o mockup de referência (`docs/models images/trocar senha.png`) mostra um ícone
  dentro de cada campo (ex.: cadeado) e um ícone de olho pra mostrar/ocultar a senha digitada —
  nenhum dos dois existe em `PciInputComponent` hoje (só `label`/`placeholder`/`hint`/`type`).
- **Onde teria uso**: `trocar-senha-form.html` e qualquer outro `pci-input type="password"`.
- **Por que não foi aplicado ainda**: não dá pra injetar um ícone dentro do `<input>` nativo do
  componente sem alcançar o DOM interno por fora (`::ng-deep`), o que é frágil e specific demais
  pra manter localmente — igual ao caso já registrado do autofill (acima), mas esse é puramente
  cosmético, sem justificar o mesmo nível de gambiarra.
- **Spec pra portar**: `PciInputComponent` ganhar um input opcional `icon`/slot de prefixo, e
  `type="password"` ganhar automaticamente o botão de alternância mostrar/ocultar (com o próprio
  ícone `eye`/`eye-off` que a lib já tem em `PciIconName`).

## `PciFeedbackModalService` não avisa quando o modal fecha

- **Por quê**: `showSuccess(message, title?)`/`showError(...)` são `void` — não devolvem
  `Observable`/`Promise` nem qualquer outro jeito de saber quando o usuário fechou o modal. O
  modal é um overlay fixo (`position:fixed;inset:0`) que só fecha no clique do usuário — encadear
  outra ação logo depois de chamar `showSuccess(...)` faz os dois overlays ficarem visíveis ao
  mesmo tempo, um por cima do outro.
- **Onde foi contornado**: `frontend/src/app/features/admin/pages/servidor-form/servidor-form.ts`
  — ao criar um servidor, `save()` mostra "Servidor criado com sucesso." e só chama
  `perguntarCriarUsuario(created)` (que abre o modal "Cadastrar usuário?") depois que o usuário
  fecha o primeiro. Como a versão publicada do pacote não expõe isso, o workaround observa
  diretamente `PciFeedbackModalService.state` (o signal já é público): `toObservable(this.feedback.state)`
  guardado como campo da classe (criado em contexto de injeção), filtrando até `state.open` virar
  `false` (a lib nunca zera `state` pra `null` no `close()`, só marca `open: false`) antes de
  seguir pro próximo modal.
- **Spec pra portar**: já implementado do lado da lib nesta mesma leva de mudanças —
  `showSuccess`/`showError` passam a devolver `Observable<void>` que emite quando o modal fecha
  (`lib/components/feedback-modal/pci-feedback-modal.service.ts`, repositório
  `c:\Users\Fani\Documents\gestao-PCIRN\pci-design-system`). Quando o app atualizar pra essa nova
  versão do pacote, trocar o workaround em `servidor-form.ts` por
  `this.feedback.showSuccess(...).subscribe(() => this.perguntarCriarUsuario(created))` e remover
  o `feedbackClosed$`/`toObservable` manual.

## Como usar este documento

Sempre que algo for implementado localmente por falta de suporte no design system, registrar
aqui: o que, por quê, onde é usado, e a especificação pra reproduzir/portar o comportamento
dentro do pacote.
