import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SiteHeaderComponent } from './layout/site-header/site-header';
import { SiteFooterComponent } from './layout/site-footer/site-footer';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
