import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, TranslatePipe],
  template: `
    <section class="section">
      <div class="container state">
        <h1>404</h1>
        <p>{{ 'common.notFound' | t }}</p>
        <a class="btn" routerLink="/">{{ 'common.backHome' | t }}</a>
      </div>
    </section>
  `,
})
export class NotFoundComponent {}
