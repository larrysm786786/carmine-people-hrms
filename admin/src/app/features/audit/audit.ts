import { errorMessage } from '../../core/utils/error-message';
import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { AuditService, AuditLog } from '../../core/services/audit.service';

@Component({
  selector: 'app-audit',
  imports: [CommonModule, MatTableModule, MatPaginatorModule],
  templateUrl: './audit.html',
  styleUrl: './audit.scss',
})
export class Audit {
  private readonly auditService = inject(AuditService);

  readonly items = signal<AuditLog[]>([]);
  readonly total = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly displayedColumns = ['timestamp', 'actorName', 'action', 'entityType', 'entityId', 'ipAddress'];

  page = 1;
  pageSize = 20;

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.auditService.getList(this.page, this.pageSize);
      this.items.set(result.items);
      this.total.set(result.total);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load audit log.');
    } finally {
      this.isLoading.set(false);
    }
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }
}
