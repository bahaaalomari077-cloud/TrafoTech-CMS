import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home').then((m) => m.HomeComponent),
  },
  {
    path: 'services',
    loadComponent: () => import('./pages/services/services').then((m) => m.ServicesComponent),
  },
  {
    path: 'services/:slug',
    loadComponent: () =>
      import('./pages/service-detail/service-detail').then((m) => m.ServiceDetailComponent),
  },
  {
    path: 'blog',
    loadComponent: () => import('./pages/blog/blog').then((m) => m.BlogComponent),
  },
  {
    path: 'blog/:slug',
    loadComponent: () =>
      import('./pages/blog-detail/blog-detail').then((m) => m.BlogDetailComponent),
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
