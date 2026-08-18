import { Component, input } from '@angular/core';
import { ContactInfoBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-contact-info',
  templateUrl: './contact-info.html',
  styleUrl: './contact-info.scss',
})
export class ContactInfoComponent {
  readonly block = input.required<ContactInfoBlock>();
}
