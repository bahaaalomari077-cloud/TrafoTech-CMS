import { Component, input } from '@angular/core';
import { StoryBlock } from '../../core/models/blocks.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';
import { CountUpDirective } from '../../core/count-up.directive';

@Component({
  selector: 'app-story-block',
  imports: [SafeHtmlPipe, CountUpDirective],
  templateUrl: './story-block.html',
  styleUrl: './story-block.scss',
})
export class StoryBlockComponent {
  readonly block = input.required<StoryBlock>();
}
