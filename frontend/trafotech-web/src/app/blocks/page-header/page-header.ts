import { Component, input } from '@angular/core';
import { PageHeaderBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeaderComponent {
  readonly block = input.required<PageHeaderBlock>();
}
