import { registerLocaleData } from '@angular/common';
import localeAr from '@angular/common/locales/ar';
import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { appConfig } from './app/app.config';

// The DatePipe is called with an explicit 'ar' locale, which throws unless registered up front.
registerLocaleData(localeAr, 'ar');

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
