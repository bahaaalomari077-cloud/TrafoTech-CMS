import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeroBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-hero-block',
  imports: [RouterLink],
  templateUrl: './hero-block.html',
  styleUrl: './hero-block.scss',
})
export class HeroBlockComponent {
  readonly block = input.required<PageHeroBlock>();

  protected href(url: string | null | undefined): string {
    return url || '/';
  }
}
