import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  message: string;
  /** Text of the button that confirms, "Löschen" by default */
  confirmLabel?: string;
}

/** Asks before something is deleted, the dialog itself is part of the app shell. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly request = signal<ConfirmRequest | null>(null);
  private resolve?: (confirmed: boolean) => void;

  ask(request: ConfirmRequest): Promise<boolean> {
    this.resolve?.(false);
    this.request.set(request);
    return new Promise(resolve => this.resolve = resolve);
  }

  answer(confirmed: boolean): void {
    this.resolve?.(confirmed);
    this.resolve = undefined;
    this.request.set(null);
  }
}
