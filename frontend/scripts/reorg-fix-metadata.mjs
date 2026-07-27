/**
 * Atualiza selectors, templateUrl/styleUrl, nomes de classe e imports
 * após a reorganização de pastas das features.
 */
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('src/app');

const pages = [
  // [file, selector, className, html, scss?]
  ['features/escalas/pages/escala-list/escala-list.ts', 'app-escala-list', 'EscalaList', 'escala-list.html', 'escala-list.scss'],
  ['features/escalas/pages/escala-form/escala-form.ts', 'app-escala-form', 'EscalaForm', 'escala-form.html', 'escala-form.scss'],
  ['features/escalas/pages/escala-detail/escala-detail.ts', 'app-escala-detail', 'EscalaDetail', 'escala-detail.html', 'escala-detail.scss'],
  ['features/escalas/pages/escala-calendario/escala-calendario.ts', 'app-escala-calendario', 'EscalaCalendario', 'escala-calendario.html', 'escala-calendario.scss'],
  ['features/escalas/pages/escala-copiar/escala-copiar.ts', 'app-escala-copiar', 'EscalaCopiar', 'escala-copiar.html', null],
  ['features/escalas/components/escala-matrix/escala-matrix.ts', 'app-escala-matrix', 'EscalaMatrix', 'escala-matrix.html', 'escala-matrix.scss'],
  ['features/escalas/components/afastamento-dialog/afastamento-dialog.ts', 'app-afastamento-dialog', 'AfastamentoDialog', 'afastamento-dialog.html', null],
  ['features/afastamentos/pages/afastamento-list/afastamento-list.ts', 'app-afastamento-list', 'AfastamentoList', 'afastamento-list.html', 'afastamento-list.scss'],
  ['features/afastamentos/pages/afastamento-form/afastamento-form.ts', 'app-afastamento-form', 'AfastamentoForm', 'afastamento-form.html', null],
  ['features/admin/pages/servidor-list/servidor-list.ts', 'app-servidor-list', 'ServidorList', 'servidor-list.html', 'servidor-list.scss'],
  ['features/admin/pages/servidor-form/servidor-form.ts', 'app-servidor-form', 'ServidorForm', 'servidor-form.html', 'servidor-form.scss'],
  ['features/admin/pages/usuario-list/usuario-list.ts', 'app-usuario-list', 'UsuarioList', 'usuario-list.html', null],
  ['features/admin/pages/usuario-form/usuario-form.ts', 'app-usuario-form', 'UsuarioForm', 'usuario-form.html', 'usuario-form.scss'],
  ['features/admin/pages/perfil-list/perfil-list.ts', 'app-perfil-list', 'PerfilList', 'perfil-list.html', 'perfil-list.scss'],
  ['features/admin/pages/perfil-form/perfil-form.ts', 'app-perfil-form', 'PerfilForm', 'perfil-form.html', 'perfil-form.scss'],
  ['features/admin/pages/estrutura-list/estrutura-list.ts', 'app-estrutura-list', 'EstruturaList', 'estrutura-list.html', 'estrutura-list.scss'],
  ['features/admin/pages/nucleo-form/nucleo-form.ts', 'app-nucleo-form', 'NucleoForm', 'nucleo-form.html', 'nucleo-form.scss'],
  ['features/admin/pages/setor-form/setor-form.ts', 'app-setor-form', 'SetorForm', 'setor-form.html', 'setor-form.scss'],
  ['features/gestao-setor/pages/solicitacao-troca-list/solicitacao-troca-list.ts', 'app-solicitacao-troca-list', 'SolicitacaoTrocaList', 'solicitacao-troca-list.html', 'solicitacao-troca-list.scss'],
  ['features/auth/pages/login-form/login-form.ts', 'app-login-form', 'LoginForm', 'login-form.html', null],
  ['features/auth/pages/trocar-senha-form/trocar-senha-form.ts', 'app-trocar-senha-form', 'TrocarSenhaForm', 'trocar-senha-form.html', 'trocar-senha-form.scss'],
  ['features/home/pages/home/home.ts', 'app-home', 'Home', 'home.html', 'home.scss'],
  ['features/not-found/pages/not-found/not-found.ts', 'app-not-found', 'NotFound', 'not-found.html', 'not-found.scss'],
];

const classRenames = [
  ['EscalasPageComponent', 'EscalaList'],
  ['EscalaWizardPageComponent', 'EscalaForm'],
  ['EscalaDetailPageComponent', 'EscalaDetail'],
  ['EscalaCalendarioPageComponent', 'EscalaCalendario'],
  ['EscalaCopiarPageComponent', 'EscalaCopiar'],
  ['EscalaMatrixViewComponent', 'EscalaMatrix'],
  ['AfastamentoDialogComponent', 'AfastamentoDialog'],
  ['AfastamentosPageComponent', 'AfastamentoList'],
  ['AfastamentoFormPageComponent', 'AfastamentoForm'],
  ['ServidoresPageComponent', 'ServidorList'],
  ['ServidorFormPageComponent', 'ServidorForm'],
  ['UsuariosPageComponent', 'UsuarioList'],
  ['UsuarioFormPageComponent', 'UsuarioForm'],
  ['PerfisPageComponent', 'PerfilList'],
  ['PerfilFormPageComponent', 'PerfilForm'],
  ['EstruturaOrganizacionalPageComponent', 'EstruturaList'],
  ['NucleoFormPageComponent', 'NucleoForm'],
  ['SetorFormPageComponent', 'SetorForm'],
  ['SolicitacoesTrocasPageComponent', 'SolicitacaoTrocaList'],
  ['LoginComponent', 'LoginForm'],
  ['TrocarSenhaPageComponent', 'TrocarSenhaForm'],
  ['HomeComponent', 'Home'],
  ['NotFoundComponent', 'NotFound'],
  ['ConfirmDialogComponent', 'ConfirmDialog'],
  ['PromptDialogComponent', 'PromptDialog'],
];

function updatePageMetadata(rel, selector, className, html, scss) {
  const file = path.join(root, rel);
  let src = fs.readFileSync(file, 'utf8');

  src = src.replace(/selector:\s*'[^']*'/, `selector: '${selector}'`);
  src = src.replace(/templateUrl:\s*'[^']*'/, `templateUrl: './${html}'`);
  if (scss) {
    if (/styleUrl:\s*'[^']*'/.test(src)) {
      src = src.replace(/styleUrl:\s*'[^']*'/, `styleUrl: './${scss}'`);
    } else if (/styleUrls:\s*\[[^\]]*\]/.test(src)) {
      src = src.replace(/styleUrls:\s*\[[^\]]*\]/, `styleUrl: './${scss}'`);
    }
  }

  for (const [from, to] of classRenames) {
    src = src.replaceAll(from, to);
  }

  fs.writeFileSync(file, src);
  console.log('meta', rel);
}

for (const row of pages) {
  updatePageMetadata(...row);
}

// Walk all .ts under src/app and apply class renames + path fixes
function walk(dir, out = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(p, out);
    else if (entry.name.endsWith('.ts')) out.push(p);
  }
  return out;
}

const pathReplacements = [
  // dialog helpers
  ["./confirm-dialog.component", "./confirm-dialog/confirm-dialog"],
  ["./prompt-dialog.component", "./prompt-dialog/prompt-dialog"],
  // escalas components
  ["../components/escala-matrix-view.component", "../../components/escala-matrix/escala-matrix"],
  ["./pages/escala-wizard-page.component", "./pages/escala-form/escala-form"],
  ["./pages/escalas-page.component", "./pages/escala-list/escala-list"],
  ["./pages/escala-detail-page.component", "./pages/escala-detail/escala-detail"],
  ["./pages/escala-calendario-page.component", "./pages/escala-calendario/escala-calendario"],
  ["./pages/escala-copiar-page.component", "./pages/escala-copiar/escala-copiar"],
  ["../components/afastamento-dialog.component", "../../components/afastamento-dialog/afastamento-dialog"],
  ["./components/afastamento-dialog.component", "./components/afastamento-dialog/afastamento-dialog"],
  ["./components/escala-matrix-view.component", "./components/escala-matrix/escala-matrix"],
  // afastamentos feature move
  ["../../gestao-setor/services/afastamentos-api.service", "../../afastamentos/services/afastamentos-api.service"],
  ["../../../gestao-setor/services/afastamentos-api.service", "../../../afastamentos/services/afastamentos-api.service"],
  ["../../afastamentos-route-pages", "../../afastamentos.routes.meta"],
  ["../afastamentos-route-pages", "../afastamentos.routes.meta"],
  // auth/home/not-found routes in app.routes
  ["./features/auth/pages/login/login.component", "./features/auth/pages/login-form/login-form"],
  ["./features/auth/pages/trocar-senha/trocar-senha-page.component", "./features/auth/pages/trocar-senha-form/trocar-senha-form"],
  ["./features/not-found/pages/not-found/not-found.component", "./features/not-found/pages/not-found/not-found"],
  ["./pages/home/home.component", "./pages/home/home"],
  // admin routes
  ["./pages/usuarios/usuarios-page.component", "./pages/usuario-list/usuario-list"],
  ["./pages/usuarios/usuario-form-page.component", "./pages/usuario-form/usuario-form"],
  ["./pages/perfis/perfis-page.component", "./pages/perfil-list/perfil-list"],
  ["./pages/perfis/perfil-form-page.component", "./pages/perfil-form/perfil-form"],
  ["./pages/servidores/servidores-page.component", "./pages/servidor-list/servidor-list"],
  ["./pages/servidores/servidor-form-page.component", "./pages/servidor-form/servidor-form"],
  ["./pages/estrutura/estrutura-organizacional-page.component", "./pages/estrutura-list/estrutura-list"],
  ["./pages/estrutura/nucleo-form-page.component", "./pages/nucleo-form/nucleo-form"],
  ["./pages/estrutura/setor-form-page.component", "./pages/setor-form/setor-form"],
  // gestao-setor
  ["./pages/afastamentos/afastamentos-page.component", "../afastamentos/pages/afastamento-list/afastamento-list"],
  ["./pages/afastamentos/afastamento-form-page.component", "../afastamentos/pages/afastamento-form/afastamento-form"],
  ["./pages/solicitacoes-trocas-page.component", "./pages/solicitacao-troca-list/solicitacao-troca-list"],
];

// Depth fix: escalas pages that gained one folder level
const depthFixFiles = [
  'features/escalas/pages/escala-list/escala-list.ts',
  'features/escalas/pages/escala-form/escala-form.ts',
  'features/escalas/pages/escala-detail/escala-detail.ts',
  'features/escalas/pages/escala-calendario/escala-calendario.ts',
  'features/escalas/pages/escala-copiar/escala-copiar.ts',
];

for (const rel of depthFixFiles) {
  const file = path.join(root, rel);
  let src = fs.readFileSync(file, 'utf8');
  // ../../../X → ../../../../X for core/shared/environments (one more level)
  src = src.replace(
    /from '(\.\.\/){3}(core|shared|environments)\//g,
    "from '../../../../$2/",
  );
  // ../services → ../../services ; ../models → ../../models
  src = src.replace(/from '\.\.\/(services|models)\//g, "from '../../$1/");
  fs.writeFileSync(file, src);
  console.log('depth', rel);
}

// components under escalas/components also gained a folder
for (const rel of [
  'features/escalas/components/escala-matrix/escala-matrix.ts',
  'features/escalas/components/afastamento-dialog/afastamento-dialog.ts',
]) {
  const file = path.join(root, rel);
  let src = fs.readFileSync(file, 'utf8');
  src = src.replace(
    /from '(\.\.\/){3}(core|shared|environments)\//g,
    "from '../../../../$2/",
  );
  src = src.replace(/from '\.\.\/\.\.\/(services|models)\//g, "from '../../../$1/");
  src = src.replace(
    /from '\.\.\/\.\.\/gestao-setor\/services\/afastamentos-api\.service'/g,
    "from '../../../afastamentos/services/afastamentos-api.service'",
  );
  fs.writeFileSync(file, src);
  console.log('depth-comp', rel);
}

for (const file of walk(root)) {
  let src = fs.readFileSync(file, 'utf8');
  let next = src;
  for (const [from, to] of classRenames) {
    next = next.replaceAll(from, to);
  }
  for (const [from, to] of pathReplacements) {
    next = next.replaceAll(from, to);
  }
  // HTML selectors used in templates
  next = next.replaceAll('app-escala-matrix-view', 'app-escala-matrix');
  next = next.replaceAll('app-escala-wizard-page', 'app-escala-form');
  if (next !== src) {
    fs.writeFileSync(file, next);
    console.log('rewrite', path.relative(root, file));
  }
}

console.log('done');
