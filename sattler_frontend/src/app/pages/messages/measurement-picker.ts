import { Component, effect, inject, input, output, signal } from '@angular/core';
import { Horse } from '../../models/horse.model';
import { BackendApiService, MeasurementSummary } from '../../services/backend-api.service';

/**
 * Selects a measurement to send in the chat. Only the horses of the customer of the chat are offered,
 * the backend does not allow sending measurements of other customers either.
 */
@Component({
  selector: 'app-measurement-picker',
  standalone: true,
  template: `
    <div class="picker" role="dialog" aria-label="Messung senden">
      <header>
        <strong>Messung senden</strong>
        <button type="button" class="close" aria-label="Schließen" (click)="cancel.emit()">
          <span class="material-icons">close</span>
        </button>
      </header>

      @if (horsesLoading()) {
        <p class="hint" role="status">Pferde werden geladen …</p>
      } @else if (!horses().length) {
        <p class="hint">Diese Kundin bzw. dieser Kunde hat noch keine Pferde.</p>
      } @else {
        <label>
          <span>Pferd</span>
          <select [value]="horseId()" (change)="selectHorse($any($event.target).value)">
            <option value="" disabled>Pferd auswählen</option>
            @for (horse of horses(); track horse.id) {
              <option [value]="horse.id">{{ horse.name }}</option>
            }
          </select>
        </label>

        @if (horseId()) {
          @if (measurementsLoading()) {
            <p class="hint" role="status">Messungen werden geladen …</p>
          } @else {
            <ul>
              @for (measurement of measurements(); track measurement.id) {
                <li>
                  <button type="button" [disabled]="sending()" (click)="selected.emit(measurement.id)">
                    <span class="material-icons" aria-hidden="true">analytics</span>
                    <span class="text">
                      <strong>Messung vom {{ germanDate(measurement.date) }}</strong>
                      <small>{{ measurement.riderName }} · {{ measurement.saddleName }}</small>
                    </span>
                    <span class="material-icons send" aria-hidden="true">send</span>
                  </button>
                </li>
              } @empty {
                <p class="hint">Dieses Pferd hat noch keine Messungen.</p>
              }
            </ul>
          }
        }
      }
      @if (error()) {
        <p class="hint error" role="alert">{{ error() }}</p>
      }
    </div>
  `,
  styles: `
    .picker {
      margin: 0 18px 8px;
      padding: 12px 14px;
      border: 1px solid var(--border);
      border-radius: 12px;
      background: var(--bg-card);
      box-shadow: var(--shadow-card);
    }

    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 8px;
      color: var(--text-strong);
    }

    .close,
    li button {
      border: 0;
      background: none;
      color: inherit;
      font: inherit;
      cursor: pointer;
    }

    label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      color: var(--text-soft);
      font-size: 12px;
    }

    select {
      height: 38px;
      padding: 0 10px;
      border: 1px solid var(--border);
      border-radius: 8px;
      background: var(--bg-card);
      color: var(--text);
      font: inherit;
      font-size: 14px;
    }

    ul {
      max-height: 220px;
      margin: 8px 0 0;
      padding: 0;
      overflow-y: auto;
      list-style: none;
    }

    li button {
      display: flex;
      gap: 10px;
      align-items: center;
      width: 100%;
      padding: 8px;
      border-radius: 8px;
      color: var(--text);
      text-align: left;

      &:hover:not(:disabled) {
        background: var(--purple-50);
      }

      &:disabled {
        opacity: 0.6;
        cursor: default;
      }

      .material-icons {
        color: var(--purple-500);
        font-size: 20px;
      }
    }

    .text {
      display: flex;
      flex: 1;
      flex-direction: column;
      min-width: 0;

      small {
        color: var(--text-soft);
      }
    }

    .hint {
      margin: 6px 0;
      color: var(--text-muted);
      font-size: 13px;

      &.error {
        color: var(--accent-red);
      }
    }
  `,
})
export class MeasurementPickerComponent {
  private readonly api = inject(BackendApiService);

  readonly contactId = input.required<string>();
  readonly sending = input(false);
  readonly selected = output<string>();
  readonly cancel = output<void>();

  readonly horses = signal<Horse[]>([]);
  readonly horsesLoading = signal(false);
  readonly horseId = signal('');
  readonly measurements = signal<MeasurementSummary[]>([]);
  readonly measurementsLoading = signal(false);
  readonly error = signal('');

  constructor() {
    // only the horses of the customer of the chat
    effect(onCleanup => {
      const contactId = this.contactId();
      this.horsesLoading.set(true);
      this.horseId.set('');
      this.error.set('');
      const subscription = this.api.getHorses(contactId).subscribe({
        next: horses => {
          this.horses.set(horses);
          this.horsesLoading.set(false);
          if (horses.length === 1) this.selectHorse(horses[0].id);
        },
        error: (error: Error) => {
          this.horses.set([]);
          this.error.set(error.message);
          this.horsesLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  selectHorse(horseId: string): void {
    this.horseId.set(horseId);
    this.measurements.set([]);
    this.error.set('');
    this.measurementsLoading.set(true);
    this.api.getMeasurementSummariesOfHorse(horseId).subscribe({
      next: measurements => {
        if (this.horseId() !== horseId) return;
        this.measurements.set([...measurements].sort((left, right) => right.date.localeCompare(left.date)));
        this.measurementsLoading.set(false);
      },
      error: (error: Error) => {
        this.error.set(error.message);
        this.measurementsLoading.set(false);
      },
    });
  }

  germanDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
  }
}
