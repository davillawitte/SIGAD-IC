import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';

import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog/confirm-dialog';
import { PromptDialog, PromptDialogData } from './prompt-dialog/prompt-dialog';

const DIALOG_DEFAULTS: MatDialogConfig = {
  width: '560px',
  maxWidth: '95vw',
  panelClass: 'pci-app-dialog-panel',
  autoFocus: 'first-tabbable',
};

export function openConfirmDialog(
  dialog: MatDialog,
  data: ConfirmDialogData,
): Observable<boolean> {
  return dialog
    .open(ConfirmDialog, { ...DIALOG_DEFAULTS, data })
    .afterClosed()
    .pipe(map((v) => !!v));
}

export function openPromptDialog(
  dialog: MatDialog,
  data: PromptDialogData,
): Observable<string | null> {
  return dialog.open(PromptDialog, { ...DIALOG_DEFAULTS, data }).afterClosed();
}
