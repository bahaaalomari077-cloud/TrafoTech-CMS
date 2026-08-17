import { Component, input } from '@angular/core';
import { ValuesBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-values-block',
  templateUrl: './values-block.html',
  styleUrl: './values-block.scss',
})
export class ValuesBlockComponent {
  readonly block = input.required<ValuesBlock>();
}
