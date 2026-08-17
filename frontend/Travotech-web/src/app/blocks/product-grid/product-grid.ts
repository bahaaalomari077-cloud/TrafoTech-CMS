import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductGridBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-product-grid',
  imports: [RouterLink],
  templateUrl: './product-grid.html',
  styleUrl: './product-grid.scss',
})
export class ProductGridComponent {
  readonly block = input.required<ProductGridBlock>();

  protected href(url: string | null | undefined): string {
    return url || '/';
  }
}
