import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { SignatureDocumentService, SignatureDocument, DocumentSignature } from '../../core/services/signature-document.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-signature-documents',
  imports: [FormsModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatChipsModule],
  templateUrl: './signature-documents.html',
  styleUrl: './signature-documents.scss',
})
export class SignatureDocuments {
  private readonly documentService = inject(SignatureDocumentService);
  private readonly toast = inject(ToastService);

  readonly documents = signal<SignatureDocument[]>([]);
  readonly displayedColumns = ['title', 'status', 'signatures', 'actions'];
  readonly isLoading = signal(true);
  newTitle = '';
  newContent = '';

  readonly expandedDocumentId = signal<string | null>(null);
  readonly signatures = signal<DocumentSignature[]>([]);
  readonly isLoadingSignatures = signal(false);

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.documents.set(await this.documentService.getList());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load documents.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async create(): Promise<void> {
    if (!this.newTitle.trim() || !this.newContent.trim()) return;
    try {
      await this.documentService.create({ title: this.newTitle.trim(), content: this.newContent.trim() });
      this.newTitle = '';
      this.newContent = '';
      await this.load();
      this.toast.success('Document added for signature.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to create document.');
    }
  }

  async toggleSignatures(doc: SignatureDocument): Promise<void> {
    if (this.expandedDocumentId() === doc.id) {
      this.expandedDocumentId.set(null);
      return;
    }
    this.expandedDocumentId.set(doc.id);
    this.isLoadingSignatures.set(true);
    try {
      this.signatures.set(await this.documentService.getSignatures(doc.id));
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load signatures.');
    } finally {
      this.isLoadingSignatures.set(false);
    }
  }

  async close(doc: SignatureDocument): Promise<void> {
    try {
      await this.documentService.close(doc.id);
      await this.load();
      this.toast.success('Document closed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to close document.');
    }
  }
}
