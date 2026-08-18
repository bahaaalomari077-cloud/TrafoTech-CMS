import { Directive, ElementRef, OnDestroy, OnInit, inject } from '@angular/core';

/**
 * Moves the element to the end of &lt;body&gt; while it exists.
 *
 * A `position: fixed` overlay is positioned against the nearest ancestor that
 * has a transform, not the viewport — and every section on this site carries
 * one for the page-flip animation. That sized the lightbox to its section and
 * let the image spill out of the backdrop. Relocating the node escapes the
 * transformed subtree, so fixed means fixed again.
 */
@Directive({
  selector: '[appBodyPortal]',
})
export class BodyPortalDirective implements OnInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);

  ngOnInit(): void {
    document.body.appendChild(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    // Angular removes the element through its original parent, which no longer
    // holds it, so it is taken out here instead.
    this.host.nativeElement.remove();
  }
}
