import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './not-found.html',
})
export class NotFoundComponent {}
