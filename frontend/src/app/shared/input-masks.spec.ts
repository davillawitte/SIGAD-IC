import { describe, expect, it } from 'vitest';

import {
  digitsOnly,
  formatCpfDisplay,
  isCpfComplete,
  isEmailValid,
  isMatriculaValid,
  isTelefoneValid,
  maskCpf,
  maskMatricula,
  maskTelefone,
} from './input-masks';

describe('input-masks', () => {
  describe('digitsOnly', () => {
    it('remove tudo que nao e digito', () => {
      expect(digitsOnly('a1b2c3')).toBe('123');
      expect(digitsOnly(null)).toBe('');
      expect(digitsOnly(undefined)).toBe('');
    });
  });

  describe('maskCpf', () => {
    it('aplica a mascara progressiva ate 000.000.000-00', () => {
      expect(maskCpf('123')).toBe('123');
      expect(maskCpf('123456')).toBe('123.456');
      expect(maskCpf('123456789')).toBe('123.456.789');
      expect(maskCpf('12345678901')).toBe('123.456.789-01');
    });

    it('ignora caracteres extras alem de 11 digitos', () => {
      expect(maskCpf('123456789012345')).toBe('123.456.789-01');
    });
  });

  describe('isCpfComplete', () => {
    it('exige exatamente 11 digitos', () => {
      expect(isCpfComplete('123.456.789-01')).toBe(true);
      expect(isCpfComplete('123')).toBe(false);
    });
  });

  describe('maskMatricula', () => {
    it('aceita formato padrao xxx.xxx-x', () => {
      expect(maskMatricula('1234567')).toBe('123.456-7');
    });

    it('aceita formatos legados menores', () => {
      expect(maskMatricula('1234')).toBe('1.234');
      // A partir de 5 digitos o hifen entra (ultimo digito); legado xx.xxx-x / x.xxx-x.
      expect(maskMatricula('12345')).toBe('1.234-5');
      expect(maskMatricula('123456')).toBe('12.345-6');
    });
  });

  describe('isMatriculaValid', () => {
    it('valida padrao e legado com hifen final', () => {
      expect(isMatriculaValid('123.456-7')).toBe(true);
      expect(isMatriculaValid('12.345-6')).toBe(true);
      expect(isMatriculaValid('1.234-5')).toBe(true);
      expect(isMatriculaValid('1234567')).toBe(false);
    });
  });

  describe('maskTelefone', () => {
    it('formata fixo 10 digitos e celular 11 digitos', () => {
      expect(maskTelefone('8432123456')).toBe('(84) 3212-3456');
      expect(maskTelefone('84991234567')).toBe('(84) 99123-4567');
    });
  });

  describe('isTelefoneValid', () => {
    it('vazio e valido; 10 ou 11 digitos tambem', () => {
      expect(isTelefoneValid('')).toBe(true);
      expect(isTelefoneValid(null)).toBe(true);
      expect(isTelefoneValid('(84) 3212-3456')).toBe(true);
      expect(isTelefoneValid('(84) 99123-4567')).toBe(true);
      expect(isTelefoneValid('123')).toBe(false);
    });
  });

  describe('isEmailValid', () => {
    it('rejeita vazio e formatos invalidos', () => {
      expect(isEmailValid('')).toBe(false);
      expect(isEmailValid('a@b')).toBe(false);
      expect(isEmailValid('user@example.com')).toBe(true);
    });
  });

  describe('formatCpfDisplay', () => {
    it('reaproveita maskCpf', () => {
      expect(formatCpfDisplay('12345678901')).toBe('123.456.789-01');
    });
  });
});
