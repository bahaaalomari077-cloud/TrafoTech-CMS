import {
  AfterViewInit,
  Directive,
  ElementRef,
  NgZone,
  OnDestroy,
  inject,
  input,
} from '@angular/core';

/**
 * Counts a figure up to its final value the first time it scrolls into view.
 *
 * The value stays authored as plain text in the CMS — "99.99", "<5", "24/7" —
 * and any surrounding symbols are preserved while only the digits animate. The
 * element's own text is the source, so nothing extra is stored per stat.
 */
@Directive({
  selector: '[appCountUp]',
})
export class CountUpDirective implements AfterViewInit, OnDestroy {
  /** Milliseconds for the full count. */
  readonly duration = input(1600, { alias: 'appCountUpDuration' });

  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly zone = inject(NgZone);

  private observer?: IntersectionObserver;
  private frame = 0;

  ngAfterViewInit(): void {
    const el = this.host.nativeElement;
    const source = (el.textContent ?? '').trim();

    // Split into everything before the number, the number, and everything
    // after: "<5s" keeps its "<" and "s", "24/7" only animates the 24.
    const match = source.match(/^(\D*)(\d+(?:\.\d+)?)(.*)$/s);
    if (!match) {
      return;
    }

    const [, prefix, digits, suffix] = match;
    const target = parseFloat(digits);
    const decimals = digits.includes('.') ? digits.split('.')[1].length : 0;

    if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    // Start from zero so the first paint does not flash the final value.
    el.textContent = `${prefix}${(0).toFixed(decimals)}${suffix}`;

    this.zone.runOutsideAngular(() => {
      this.observer = new IntersectionObserver(
        (entries) => {
          for (const entry of entries) {
            if (entry.isIntersecting) {
              this.observer?.disconnect();
              this.run(prefix, suffix, target, decimals);
            }
          }
        },
        { threshold: 0.4 },
      );
      this.observer.observe(el);
    });
  }

  private run(prefix: string, suffix: string, target: number, decimals: number): void {
    const el = this.host.nativeElement;
    const total = this.duration();
    const started = performance.now();

    const step = (now: number) => {
      const t = Math.min(1, (now - started) / total);
      // Ease-out cubic: fast at first, settling gently onto the final figure —
      // the deceleration the reference shows.
      const eased = 1 - Math.pow(1 - t, 3);
      el.textContent = `${prefix}${(target * eased).toFixed(decimals)}${suffix}`;

      if (t < 1) {
        this.frame = requestAnimationFrame(step);
      } else {
        this.frame = 0;
      }
    };

    this.frame = requestAnimationFrame(step);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    if (this.frame) {
      cancelAnimationFrame(this.frame);
    }
  }
}
