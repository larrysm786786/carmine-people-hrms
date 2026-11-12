import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AiAssistantService, AiChatMessage } from '../../core/services/ai-assistant.service';

@Component({
  selector: 'app-ai-insights',
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, MatProgressBarModule],
  templateUrl: './ai-insights.html',
  styleUrl: './ai-insights.scss',
})
export class AiInsights {
  private readonly aiAssistantService = inject(AiAssistantService);
  @ViewChild('scrollAnchor') private scrollAnchor?: ElementRef<HTMLDivElement>;

  readonly messages = signal<AiChatMessage[]>([]);
  readonly isSending = signal(false);
  readonly isConfigured = signal<boolean | null>(null);
  input = '';

  constructor() {
    this.checkConfigured();
  }

  // Minimal, safe markdown subset for AI replies: **bold** only. Escape first so the AI's
  // own output can never inject markup — [innerHTML] only ever sees the <strong> tags added here.
  formatMessage(content: string): string {
    const escaped = content.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    return escaped.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  }

  async checkConfigured(): Promise<void> {
    this.isConfigured.set(await this.aiAssistantService.isConfigured());
  }

  async send(): Promise<void> {
    const text = this.input.trim();
    if (!text || this.isSending()) return;

    const history = this.messages();
    this.messages.set([...history, { role: 'user', content: text }]);
    this.input = '';
    this.isSending.set(true);
    this.scrollToEnd();

    try {
      const reply = await this.aiAssistantService.sendMessage(text, history);
      this.messages.set([...this.messages(), { role: 'assistant', content: reply }]);
    } catch {
      this.messages.set([...this.messages(), { role: 'assistant', content: 'Sorry, something went wrong. Please try again.' }]);
    } finally {
      this.isSending.set(false);
      this.scrollToEnd();
    }
  }

  private scrollToEnd(): void {
    setTimeout(() => this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' }), 50);
  }
}
