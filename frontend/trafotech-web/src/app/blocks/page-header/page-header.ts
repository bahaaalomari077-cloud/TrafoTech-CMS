import { Component, input } from '@angular/core';
import { PageHeaderBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-page-header',
  template: `
    <div class="page-header">
      @if (block().eyebrow) {
        <span class="eyebrow">{{ block().eyebrow }}</span>
      }
      @if (block().heading) {
        <h1>{{ block().heading }}</h1>
      }
      @if (block().lead) {
        <p class="lead">{{ block().lead }}</p>
      }
    </div>
  `,
  styles: `
    .page-header {
      max-width: 62ch;
    }

    .eyebrow {
      color: var(--navy-400);
    }

    h1 {
      color: #fff;
      margin-bottom: 0.5em;
    }

    .lead {
      font-size: var(--step-1);
      color: var(--navy-300);
      margin: 0;
    }
  `,
})
export class PageHeaderComponent {
  readonly block = input.required<PageHeaderBlock>();
}
