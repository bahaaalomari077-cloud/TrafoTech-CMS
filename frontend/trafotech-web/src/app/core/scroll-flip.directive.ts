import {
  AfterViewInit,
  Directive,
  ElementRef,
  NgZone,
  OnDestroy,
  inject,
} from '@angular/core';

/**
 * Drives the scroll-linked 3D flip.
 *
 * Sets a single custom property, --flip-p, from 0 to 1 as the element crosses
 * the viewport; the transform itself is expressed in CSS. Work runs outside
 * Angular and is throttled to one rAF per scroll burst, so a page of sections
 * costs one style write each per frame rather than a change-detection pass.
 */
@Directive({
  selector: '[appScrollFlip]',
})
export class ScrollFlipDirective implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly zone = inject(NgZone);

  private frame = 0;
  private listening = false;
  private readonly onScroll = () => this.schedule();

  ngAfterViewInit(): void {
    // Honour the user's motion preference: leave the element at its resting
    // state and never attach a scroll listener.
    if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
      this.host.nativeElement.style.setProperty('--flip-p', '1');
      return;
    }

    this.zone.runOutsideAngular(() => {
      addEventListener('scroll', this.onScroll, { passive: true });
      addEventListener('resize', this.onScroll, { passive: true });
      this.listening = true;
      this.update();
    });
  }

  private schedule(): void {
    if (this.frame) {
      return;
    }
    this.frame = requestAnimationFrame(() => {
      this.frame = 0;
      this.update();
    });
  }

  private update(): void {
    const el = this.host.nativeElement;
    const rect = el.getBoundingClientRect();
    const vh = innerHeight || 1;

    // Mapped from the reference's measured curve: the turn starts as the top
    // reaches ~viewport height and completes just before it reaches ~8% of it,
    // so a section is flat well before the reader is reading it. The window is
    // deliberately wide — narrowing it makes each section settle early, which
    // reads as the turn snapping shut rather than easing.
    const start = vh;
    const end = vh * 0.08;
    const progress = (start - rect.top) / (start - end);
    const clamped = Math.min(1, Math.max(0, progress));

    el.style.setProperty('--flip-p', clamped.toFixed(4));

    // Drop the 3D layer entirely at rest — see .is-settled. Text composited on
    // a promoted layer renders soft, which reads as the figures wobbling.
    el.classList.toggle('is-settled', clamped >= 1);
  }

  ngOnDestroy(): void {
    if (this.listening) {
      removeEventListener('scroll', this.onScroll);
      removeEventListener('resize', this.onScroll);
    }
    if (this.frame) {
      cancelAnimationFrame(this.frame);
    }
  }
}
