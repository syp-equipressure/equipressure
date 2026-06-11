import { Component, Input, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../shared/page-header/page-header';

@Component({
  selector: 'app-stub-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './stub.page.html',
  styleUrl: './stub.page.scss',
})
export class StubPage {
  @Input() title = '';

  private readonly route = inject(ActivatedRoute);
  private readonly routeData = toSignal(this.route.data, { initialValue: {} });

  readonly resolvedTitle = computed(() => {
    const dataTitle = (this.routeData() as { title?: string }).title ?? '';
    return this.title || dataTitle;
  });
}
