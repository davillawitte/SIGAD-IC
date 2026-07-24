import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';

import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';
import { PromptDialogComponent, PromptDialogData } from './prompt-dialog.component';

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
    .open(ConfirmDialogComponent, { ...DIALOG_DEFAULTS, data })
    .afterClosed()
    .pipe(map((v) => !!v));
}

export function openPromptDialog(
  dialog: MatDialog,
  data: PromptDialogData,
): Observable<string | null> {
  return dialog.open(PromptDialogComponent, { ...DIALOG_DEFAULTS, data }).afterClosed();
}
