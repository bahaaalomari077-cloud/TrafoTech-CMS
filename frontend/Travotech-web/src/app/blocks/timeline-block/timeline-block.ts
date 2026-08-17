import { Component, input } from '@angular/core';
import { TimelineBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-timeline-block',
  templateUrl: './timeline-block.html',
  styleUrl: './timeline-block.scss',
})
export class TimelineBlockComponent {
  readonly block = input.required<TimelineBlock>();
}
