import { errorMessage } from '../../core/utils/error-message';
import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { PulseSurveyService, PulseSurvey, PulseSurveyResponse } from '../../core/services/pulse-survey.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-pulse-surveys',
  imports: [FormsModule, DecimalPipe, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatChipsModule],
  templateUrl: './pulse-surveys.html',
  styleUrl: './pulse-surveys.scss',
})
export class PulseSurveys {
  private readonly pulseSurveyService = inject(PulseSurveyService);
  private readonly toast = inject(ToastService);

  readonly surveys = signal<PulseSurvey[]>([]);
  readonly displayedColumns = ['question', 'status', 'responses', 'average', 'actions'];
  readonly isLoading = signal(true);
  newQuestion = '';

  readonly expandedSurveyId = signal<string | null>(null);
  readonly responses = signal<PulseSurveyResponse[]>([]);
  readonly isLoadingResponses = signal(false);

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.surveys.set(await this.pulseSurveyService.getList());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load pulse surveys.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async create(): Promise<void> {
    if (!this.newQuestion.trim()) return;
    try {
      await this.pulseSurveyService.create(this.newQuestion.trim());
      this.newQuestion = '';
      await this.load();
      this.toast.success('Pulse survey created.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to create pulse survey.');
    }
  }

  async toggleResponses(survey: PulseSurvey): Promise<void> {
    if (this.expandedSurveyId() === survey.id) {
      this.expandedSurveyId.set(null);
      return;
    }
    this.expandedSurveyId.set(survey.id);
    this.isLoadingResponses.set(true);
    try {
      this.responses.set(await this.pulseSurveyService.getResponses(survey.id));
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load responses.');
    } finally {
      this.isLoadingResponses.set(false);
    }
  }

  async close(survey: PulseSurvey): Promise<void> {
    try {
      await this.pulseSurveyService.close(survey.id);
      await this.load();
      this.toast.success('Survey closed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to close survey.');
    }
  }
}
