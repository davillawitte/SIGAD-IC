import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService, loginErrorMessage } from './auth.service';

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

describe('loginErrorMessage', () => {
  it('senha errada (401 com mensagem da API) mostra a mensagem da API', () => {
    expect(loginErrorMessage({ status: 401, error: { message: 'Usuário ou senha inválidos.' } })).toBe(
      'Usuário ou senha inválidos.',
    );
  });

  it('bloqueio por excesso de tentativas não é confundido com senha errada', () => {
    expect(loginErrorMessage({ status: 429, error: null })).toBe(
      'Muitas tentativas seguidas. Aguarde 1 minuto e tente novamente.',
    );
    // nginx de borda antigo devolvia 503 com página HTML
    expect(loginErrorMessage({ status: 503, error: null })).toBe(
      'Muitas tentativas seguidas. Aguarde 1 minuto e tente novamente.',
    );
  });

  it('erro de servidor e erro de validação têm mensagem própria', () => {
    expect(loginErrorMessage({ status: 500, error: null })).toBe('Erro no servidor. Tente novamente em instantes.');
    expect(loginErrorMessage({ status: 400, error: { errors: ['Login é obrigatório.'] } })).toBe(
      'Login é obrigatório.',
    );
    expect(loginErrorMessage({ status: 0 })).toBe('Não foi possível conectar à API.');
  });
});
