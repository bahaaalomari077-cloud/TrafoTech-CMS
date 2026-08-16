import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CountUpDirective } from '../../core/count-up.directive';
import { PageHeroBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-hero-block',
  imports: [RouterLink, CountUpDirective],
  templateUrl: './hero-block.html',
  styleUrl: './hero-block.scss',
})
export class HeroBlockComponent {
  readonly block = input.required<PageHeroBlock>();

  protected href(url: string | null | undefined): string {
    return url || '/';
  }

  /**
   * Splits a headline into words so each can animate in on its own delay.
   * Done here rather than in the CMS so the editor's field stays plain text.
   */
  protected words(text: string | null | undefined): string[] {
    return (text ?? '').trim().split(/\s+/).filter(Boolean);
  }
}
