import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidenavService {
  readonly open = signal(false);

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
