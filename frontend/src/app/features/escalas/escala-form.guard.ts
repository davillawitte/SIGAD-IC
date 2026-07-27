import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

import { EscalaForm } from './pages/escala-form/escala-form';

export const escalaFormCanDeactivate: CanDeactivateFn<EscalaForm> = (
  component,
): boolean | Observable<boolean> => component.canDeactivate();
