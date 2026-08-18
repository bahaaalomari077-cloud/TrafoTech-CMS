import { AfterViewInit, Directive, ElementRef, inject } from '@angular/core';

/**
 * Moves focus onto the element once it appears.
 *
 * A lightbox that opens without focus leaves the keyboard on the page behind
 * it, so Escape and the arrow keys do nothing until the user clicks first.
 */
@Directive({
  selector: '[appAutofocus]',
})
export class AutofocusDirective implements AfterViewInit {
  private readonly host = inject(ElementRef<HTMLElement>);

  ngAfterViewInit(): void {
    this.host.nativeElement.focus?.();
  }
}
