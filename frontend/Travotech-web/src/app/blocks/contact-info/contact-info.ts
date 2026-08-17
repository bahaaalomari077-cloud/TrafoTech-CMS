import { Component, input } from '@angular/core';
import { ContactInfoBlock } from '../../core/models/blocks.models';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-contact-info',
  imports: [TranslatePipe],
  templateUrl: './contact-info.html',
  styleUrl: './contact-info.scss',
})
export class ContactInfoComponent {
  readonly block = input.required<ContactInfoBlock>();
}
