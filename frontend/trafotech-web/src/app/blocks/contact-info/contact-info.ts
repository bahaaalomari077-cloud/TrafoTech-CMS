import { Component, input } from '@angular/core';
import { ContactInfoBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-contact-info',
  template: `
    <aside class="info">
      @if (block().heading) {
        <h2>{{ block().heading }}</h2>
      }

      <ul class="channels">
        @if (block().email) {
          <li>
            <a [href]="'mailto:' + block().email">{{ block().email }}</a>
          </li>
        }
        @if (block().phone) {
          <li>
            <a [href]="'tel:' + block().phone">{{ block().phone }}</a>
          </li>
        }
      </ul>

      @if (block().note) {
        <p class="note">{{ block().note }}</p>
      }
    </aside>
  `,
  styles: `
    .info {
      padding: 1.9rem;
      border: 1px solid var(--line);
      border-radius: var(--radius-lg);
      background: var(--bg-raised);
      box-shadow: var(--shadow-sm);
      height: 100%;
    }

    h2 {
      font-size: var(--step-1);
      margin-bottom: 1.1rem;
    }

    .channels {
      margin: 0 0 1.25rem;
      padding: 0;
      list-style: none;
      display: grid;
      gap: 0.6rem;
      font-weight: 500;
    }

    .note {
      margin: 0;
      padding-top: 1.25rem;
      border-top: 1px solid var(--line);
      font-size: var(--step--1);
      color: var(--ink-muted);
    }
  `,
})
export class ContactInfoComponent {
  readonly block = input.required<ContactInfoBlock>();
}
