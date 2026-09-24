import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';

const SESSION_KEY = 'sigad-ic.auth.session';

function gravarSessao(deveAlterarSenha: boolean): void {
  localStorage.setItem(
    SESSION_KEY,
    JSON.stringify({
      accessToken: 'token-de-teste',
      expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      user: {
        login: '12345678900',
        displayName: 'Servidor de Teste',
        permissoes: [],
        perfis: [],
        setoresGerenciadosIds: [],
        nucleosGerenciadosIds: [],
        setoresDosNucleosGerenciadosIds: [],
        deveAlterarSenha,
      },
    }),
  );
}

function criarServico(): AuthService {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AuthService,
      // `restoreSession` dispara um refresh em segundo plano; aqui ele só não pode estourar.
      { provide: HttpClient, useValue: { post: vi.fn(() => of({})) } },
    ],
  });
  return TestBed.inject(AuthService);
}

describe('AuthService — sessão restaurada', () => {
  beforeEach(() => localStorage.clear());

  it('descarta a sessão de quem ainda não trocou a senha obrigatória', () => {
    gravarSessao(true);

    const auth = criarServico();

    // Fechar e reabrir a janela leva de volta ao login, não à tela de nova senha.
    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  });

  it('mantém a sessão de quem já tem a senha definida', () => {
    gravarSessao(false);

    const auth = criarServico();

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.deveAlterarSenha()).toBe(false);
  });
});
