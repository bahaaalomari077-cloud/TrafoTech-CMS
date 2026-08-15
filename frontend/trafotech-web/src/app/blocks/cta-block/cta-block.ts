import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CtaBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-cta-block',
  imports: [RouterLink],
  template: `
    <div class="cta-inner">
      @if (block().heading) {
        <h2>{{ block().heading }}</h2>
      }
      @if (block().text) {
        <p>{{ block().text }}</p>
      }
      @if (block().buttonText) {
        <a class="btn" [routerLink]="block().buttonLink?.url || '/'">
          {{ block().buttonText }}
          <span class="arrow" aria-hidden="true">→</span>
        </a>
      }
    </div>
  `,
  styles: `
    .cta-inner {
      display: grid;
      justify-items: center;
      text-align: center;
      gap: 0.4rem;
      max-width: 60ch;
      margin-inline: auto;
    }

    h2 {
      color: #fff;
    }

    p {
      color: var(--navy-300);
      margin-bottom: 1.5rem;
    }
  `,
})
export class CtaBlockComponent {
  readonly block = input.required<CtaBlock>();
}
