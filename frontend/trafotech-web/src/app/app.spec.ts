import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { I18nService } from './core/i18n.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the brand and navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand-name')?.textContent).toContain('TrafoTech');
    expect(compiled.querySelectorAll('.site-nav a').length).toBe(4);
  });

  it('should translate navigation and flip direction with the language', async () => {
    const i18n = TestBed.inject(I18nService);
    i18n.setLang('ar');

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.site-nav a')?.textContent?.trim()).toBe('الرئيسية');
    expect(i18n.dir()).toBe('rtl');

    i18n.setLang('en');
    await fixture.whenStable();
    expect(compiled.querySelector('.site-nav a')?.textContent?.trim()).toBe('Home');
    expect(i18n.dir()).toBe('ltr');
  });
});
