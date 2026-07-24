import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

import { EscalaWizardPageComponent } from './pages/escala-wizard-page.component';

export const escalaWizardCanDeactivate: CanDeactivateFn<EscalaWizardPageComponent> = (
  component,
): boolean | Observable<boolean> => component.canDeactivate();
