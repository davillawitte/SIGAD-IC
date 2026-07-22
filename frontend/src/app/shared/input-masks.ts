/** Utilitários de máscara e validação para cadastro de servidor. */

export function digitsOnly(value: string | null | undefined): string {
  return (value ?? '').replace(/\D/g, '');
}

/** CPF: 000.000.000-00 */
export function maskCpf(value: string | null | undefined): string {
  const d = digitsOnly(value).slice(0, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

export function isCpfComplete(value: string | null | undefined): boolean {
  return digitsOnly(value).length === 11;
}

/**
 * Matrícula: xxx.xxx-x (padrão) ou xx.xxx-x / x.xxx-x (legado).
 * Sempre termina com hífen + 1 dígito.
 */
export function maskMatricula(value: string | null | undefined): string {
  const d = digitsOnly(value).slice(0, 7);
  if (d.length <= 3) return d;
  if (d.length === 4) return `${d[0]}.${d.slice(1)}`;
  const last = d.slice(-1);
  const middle = d.slice(-4, -1);
  const prefix = d.slice(0, -4);
  return `${prefix}.${middle}-${last}`;
}

export function isMatriculaValid(value: string | null | undefined): boolean {
  return /^\d{1,3}\.\d{3}-\d$/.test((value ?? '').trim());
}

/** Telefone BR: (00) 0000-0000 ou (00) 00000-0000 */
export function maskTelefone(value: string | null | undefined): string {
  const d = digitsOnly(value).slice(0, 11);
  if (d.length === 0) return '';
  if (d.length <= 2) return `(${d}`;
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  }
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

export function isTelefoneValid(value: string | null | undefined): boolean {
  if (!value || !value.trim()) return true;
  const len = digitsOnly(value).length;
  return len === 10 || len === 11;
}

export function isEmailValid(value: string | null | undefined): boolean {
  if (!value || !value.trim()) return false;
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function formatCpfDisplay(cpfDigits: string | null | undefined): string {
  return maskCpf(cpfDigits);
}
