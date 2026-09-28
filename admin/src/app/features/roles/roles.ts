import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RoleService, Role, Permission } from '../../core/services/role.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-roles',
  imports: [FormsModule, MatCardModule, MatCheckboxModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './roles.html',
  styleUrl: './roles.scss',
})
export class Roles {
  private readonly roleService = inject(RoleService);
  private readonly toast = inject(ToastService);

  readonly roles = signal<Role[]>([]);
  readonly permissions = signal<Permission[]>([]);
  readonly savingRoleId = signal<string | null>(null);
  readonly isLoading = signal(false);

  newRoleName = '';

  constructor() {
    this.loadAll();
  }

  async loadAll(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [roles, permissions] = await Promise.all([this.roleService.getRoles(), this.roleService.getPermissions()]);
      this.roles.set(roles);
      this.permissions.set(permissions);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load roles.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async addRole(): Promise<void> {
    if (!this.newRoleName.trim()) return;
    try {
      await this.roleService.createRole(this.newRoleName.trim());
      this.newRoleName = '';
      this.roles.set(await this.roleService.getRoles());
      this.toast.success('Role created.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to create role.');
    }
  }

  hasPermission(role: Role, code: string): boolean {
    return role.permissionCodes.includes(code);
  }

  async togglePermission(role: Role, permission: Permission): Promise<void> {
    const currentIds = this.permissions().filter((p) => role.permissionCodes.includes(p.code)).map((p) => p.id);
    const isEnabled = role.permissionCodes.includes(permission.code);
    const newIds = isEnabled ? currentIds.filter((id) => id !== permission.id) : [...currentIds, permission.id];

    this.savingRoleId.set(role.id);
    try {
      await this.roleService.setPermissions(role.id, newIds);
      this.roles.set(await this.roleService.getRoles());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update permissions.');
    } finally {
      this.savingRoleId.set(null);
    }
  }
}
