import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home').then((m) => m.HomeComponent),
  },
  {
    path: 'not-found',
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFoundComponent),
  },
  {
    // Catch-all maps any remaining URL to an Orchard Page by autoroute path.
    //
    // It matches every depth rather than a single :slug, because English pages
    // live under /en (/en/products, /en/about) and a one-segment pattern sent
    // them to the 404 page instead.
    path: '**',
    loadComponent: () => import('./pages/cms-page/cms-page').then((m) => m.CmsPageComponent),
  },
];
