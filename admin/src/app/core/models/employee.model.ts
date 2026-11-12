export interface EmployeeProfile {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  roleName: string;
  departmentId: string;
  designationId: string;
  locationId: string;
  permissions: string[];
}

export interface EmployeeListItem {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  departmentName: string;
  designationTitle: string;
  locationName: string;
  roleName: string;
  employmentStatus: number;
}

export interface EmployeeDetail {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  departmentId: string;
  departmentName: string;
  designationId: string;
  designationTitle: string;
  locationId: string;
  locationName: string;
  managerId: string | null;
  managerName: string | null;
  roleId: string;
  roleName: string;
  dateOfJoining: string;
  dateOfBirth: string | null;
  employmentStatus: number;
  profileImageUrl: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  emergencyContactRelation: string | null;
}

export interface UpdateEmployeeRequest {
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  departmentId: string;
  designationId: string;
  locationId: string;
  managerId: string | null;
  roleId: string;
  dateOfJoining: string;
  dateOfBirth: string | null;
  employmentStatus: number;
}

export interface CreateEmployeeRequest {
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber: string | null;
  departmentId: string;
  designationId: string;
  locationId: string;
  managerId: string | null;
  roleId: string;
  dateOfJoining: string;
  dateOfBirth: string | null;
}

export const EMPLOYMENT_STATUS_LABELS = ['Active', 'Inactive', 'Terminated'];
