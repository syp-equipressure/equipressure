import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidenavService {
  readonly open = signal(typeof window === 'undefined' || window.innerWidth > 900);

  toggle() {
    this.open.update(v => !v);
  }

  show() {
    this.open.set(true);
  }

  hide() {
    this.open.set(false);
  }
}
