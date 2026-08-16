import { Component, input } from '@angular/core';
import { FaqBlock } from '../../core/models/blocks.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';

@Component({
  selector: 'app-faq-block',
  imports: [SafeHtmlPipe],
  templateUrl: './faq-block.html',
  styleUrl: './faq-block.scss',
})
export class FaqBlockComponent {
  readonly block = input.required<FaqBlock>();
}
