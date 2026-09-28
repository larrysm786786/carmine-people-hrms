/// Slim profile returned inline by /auth/login and /auth/refresh
/// (EmployeeProfileDto on the backend). The full detail record used by the
/// Profile feature comes from GET /employees/{id} separately.
class EmployeeProfile {
  const EmployeeProfile({
    required this.id,
    required this.employeeCode,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.roleName,
    required this.departmentId,
    required this.designationId,
    required this.locationId,
    required this.permissions,
  });

  factory EmployeeProfile.fromJson(Map<String, dynamic> json) {
    return EmployeeProfile(
      id: json['id'] as String,
      employeeCode: json['employeeCode'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      email: json['email'] as String,
      roleName: json['roleName'] as String,
      departmentId: json['departmentId'] as String,
      designationId: json['designationId'] as String,
      locationId: json['locationId'] as String,
      permissions: (json['permissions'] as List<dynamic>? ?? const [])
          .map((e) => e as String)
          .toList(),
    );
  }

  final String id;
  final String employeeCode;
  final String firstName;
  final String lastName;
  final String email;
  final String roleName;
  final String departmentId;
  final String designationId;
  final String locationId;
  final List<String> permissions;

  String get fullName => '$firstName $lastName';

  bool hasPermission(String permission) => permissions.contains(permission);

  Map<String, dynamic> toJson() => {
        'id': id,
        'employeeCode': employeeCode,
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'roleName': roleName,
        'departmentId': departmentId,
        'designationId': designationId,
        'locationId': locationId,
        'permissions': permissions,
      };
}
