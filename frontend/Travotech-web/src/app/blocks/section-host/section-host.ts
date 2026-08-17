import { Component, ElementRef, OnDestroy, AfterViewInit, inject, input } from '@angular/core';
import { Block, BlockWidth, Section } from '../../core/models/blocks.models';
import { BlockHostComponent } from '../block-host/block-host';
import { ScrollFlipDirective } from '../../core/scroll-flip.directive';

/**
 * Renders a Section: the CMS supplies layout choices (background, width,
 * spacing) as constrained values, which map to classes here. Editors pick from
 * a fixed list, so content edits can never produce an unstyled combination.
 */
@Component({
  selector: 'app-section-host',
  imports: [BlockHostComponent, ScrollFlipDirective],
  templateUrl: './section-host.html',
  styleUrl: './section-host.scss',
})
export class SectionHostComponent implements AfterViewInit, OnDestroy {
  readonly section = input.required<Section>();

  private readonly host = inject(ElementRef<HTMLElement>);
  private observer?: IntersectionObserver;

  protected blockCount(): number {
    return this.section().blocks?.contentItems?.length ?? 0;
  }

  /**
   * The row is a grid only when it has to be. A single full-width block keeps
   * the plain flow it always had, so existing pages are byte-for-byte unchanged.
   */
  protected rowClass(): string {
    const blocks = this.section().blocks?.contentItems ?? [];
    const sized = blocks.some((b) => this.width(b) !== 'full');

    // Two unsized blocks keep the original 1.4:1 split the contact page relies on.
    if (!sized) {
      return blocks.length === 2 ? 'block-pair' : '';
    }

    return 'block-row';
  }

  protected blockClass(block: Block): string {
    return this.rowClass() === 'block-row' ? `block-col span-${this.width(block)}` : '';
  }

  private width(block: Block): BlockWidth {
    return block.blockLayout?.width || 'full';
  }

  protected sectionClass(): string {
    const s = this.section();
    return [
      'section',
      `bg-${s.background || 'default'}`,
      `space-${s.spacing || 'normal'}`,
      'page-flip',
    ].join(' ');
  }

  protected innerClass(): string {
    const width = this.section().width || 'container';
    // A full-bleed section still needs its blocks to manage their own gutters.
    // 'reveal' rides on the inner wrapper rather than the section, so the
    // content's fade-in does not fight the section's own flip opacity.
    const base =
      width === 'full'
        ? 'section-inner-full'
        : width === 'wide'
          ? 'container container-wide'
          : 'container';
    return `${base} reveal`;
  }

  ngAfterViewInit(): void {
    // The reveal now lives on the inner wrapper, not the section, so the class
    // must land there — the section's opacity belongs to the flip alone.
    const el = this.host.nativeElement.querySelector('.reveal');
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
