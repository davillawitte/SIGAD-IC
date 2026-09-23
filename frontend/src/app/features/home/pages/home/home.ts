import { Component } from '@angular/core';
import { PciAlertComponent, PciPageHeaderComponent } from '@davillawitte/pci-design-system';

@Component({
  selector: 'app-home',
  imports: [PciAlertComponent, PciPageHeaderComponent],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {}
