import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'equipressure-theme';

  readonly theme = signal<Theme>(this.readTheme());
  readonly isDark = signal(this.theme() === 'dark');

  constructor() {
    this.applyTheme(this.theme());
  }

  toggle(): void {
    const nextTheme = this.isDark() ? 'light' : 'dark';
    this.theme.set(nextTheme);
    this.isDark.set(nextTheme === 'dark');
    this.document.defaultView?.localStorage.setItem(this.storageKey, nextTheme);
    this.applyTheme(nextTheme);
  }

  private readTheme(): Theme {
    const storedTheme = this.document.defaultView?.localStorage.getItem(this.storageKey);
    return storedTheme === 'dark' ? 'dark' : 'light';
  }

  private applyTheme(theme: Theme): void {
    this.document.documentElement.dataset['theme'] = theme;
  }
}
