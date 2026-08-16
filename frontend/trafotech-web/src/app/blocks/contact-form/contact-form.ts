import { Component, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContactFormBlock } from '../../core/models/blocks.models';
import { TranslatePipe } from '../../core/translate.pipe';

/**
 * Renders the contact form defined in the CMS.
 *
 * Submission is not wired to a backend yet: the form validates and shows the
 * editor's success message, but nothing is sent. Hooking it to a mail endpoint
 * is a separate piece of work.
 */
@Component({
  selector: 'app-contact-form',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './contact-form.html',
  styleUrl: './contact-form.scss',
})
export class ContactFormComponent {
  readonly block = input.required<ContactFormBlock>();

  protected readonly name = signal('');
  protected readonly email = signal('');
  protected readonly organisation = signal('');
  protected readonly message = signal('');
  protected readonly sent = signal(false);

  protected submit(event: Event): void {
    event.preventDefault();
    this.sent.set(true);
  }
}
