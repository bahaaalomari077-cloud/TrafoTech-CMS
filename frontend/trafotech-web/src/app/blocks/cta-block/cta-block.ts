import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CtaBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-cta-block',
  imports: [RouterLink],
  templateUrl: './cta-block.html',
  styleUrl: './cta-block.scss',
})
export class CtaBlockComponent {
  readonly block = input.required<CtaBlock>();
}
