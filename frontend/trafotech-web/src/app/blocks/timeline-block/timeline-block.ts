import { Component, input } from '@angular/core';
import { TimelineBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-timeline-block',
  template: `
    <header class="section-head">
      @if (block().eyebrow) {
        <span class="eyebrow">{{ block().eyebrow }}</span>
      }
      @if (block().heading) {
        <h2>{{ block().heading }}</h2>
      }
    </header>

    @if (block().milestones?.contentItems?.length) {
      <ol class="timeline">
        @for (m of block().milestones!.contentItems; track m.contentItemId) {
          <li class="milestone">
            <span class="year">{{ m.year }}</span>
            <div class="card">
              <h3>{{ m.displayText }}</h3>
              <p>{{ m.description }}</p>
            </div>
          </li>
        }
      </ol>
    }
  `,
  styleUrl: './timeline-block.scss',
})
export class TimelineBlockComponent {
  readonly block = input.required<TimelineBlock>();
}
