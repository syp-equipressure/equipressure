import { Component, Input } from '@angular/core';
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
}
