import { Component, ElementRef, OnDestroy, AfterViewInit, inject, input } from '@angular/core';
import { Section } from '../../core/models/blocks.models';
import { BlockHostComponent } from '../block-host/block-host';

/**
 * Renders a Section: the CMS supplies layout choices (background, width,
 * spacing) as constrained values, which map to classes here. Editors pick from
 * a fixed list, so content edits can never produce an unstyled combination.
 */
@Component({
  selector: 'app-section-host',
  imports: [BlockHostComponent],
  template: `
    <section
      [class]="sectionClass()"
      [attr.id]="section().anchor || null"
    >
      <div [class]="innerClass()">
        <!-- Two blocks in one section sit side by side (form + details); any
             other count stacks, so the common single-block case is unaffected. -->
        <div [class.block-pair]="blockCount() === 2">
          @for (block of section().blocks?.contentItems || []; track block.contentItemId) {
            <app-block-host [block]="block" />
          }
        </div>
      </div>
    </section>
  `,
  styleUrl: './section-host.scss',
})
export class SectionHostComponent implements AfterViewInit, OnDestroy {
  readonly section = input.required<Section>();

  private readonly host = inject(ElementRef<HTMLElement>);
  private observer?: IntersectionObserver;

  protected blockCount(): number {
    return this.section().blocks?.contentItems?.length ?? 0;
  }

  protected sectionClass(): string {
    const s = this.section();
    return [
      'section',
      `bg-${s.background || 'default'}`,
      `space-${s.spacing || 'normal'}`,
      'reveal',
    ].join(' ');
  }

  protected innerClass(): string {
    const width = this.section().width || 'container';
    // A full-bleed section still needs its blocks to manage their own gutters.
    if (width === 'full') {
      return 'section-inner-full';
    }
    return width === 'wide' ? 'container container-wide' : 'container';
  }

  ngAfterViewInit(): void {
    const el = this.host.nativeElement.querySelector('section');
    if (!el) {
      return;
    }

    // Reveal on scroll, but never leave content hidden if the API is missing.
    if (!('IntersectionObserver' in window)) {
      el.classList.add('is-visible');
      return;
    }

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { rootMargin: '0px 0px -10% 0px', threshold: 0.05 },
    );
    this.observer.observe(el);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
