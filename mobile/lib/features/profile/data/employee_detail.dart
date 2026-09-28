class EmployeeDetail {
  const EmployeeDetail({
    required this.id,
    required this.employeeCode,
    required this.firstName,
    required this.lastName,
    required this.email,
    this.phoneNumber,
    required this.departmentName,
    required this.designationTitle,
    required this.locationName,
    this.managerName,
    required this.roleName,
    required this.dateOfJoining,
    this.profileImageUrl,
    this.emergencyContactName,
    this.emergencyContactPhone,
    this.emergencyContactRelation,
  });

  factory EmployeeDetail.fromJson(Map<String, dynamic> json) {
    return EmployeeDetail(
      id: json['id'] as String,
      employeeCode: json['employeeCode'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      email: json['email'] as String,
      phoneNumber: json['phoneNumber'] as String?,
      departmentName: json['departmentName'] as String? ?? '',
      designationTitle: json['designationTitle'] as String? ?? '',
      locationName: json['locationName'] as String? ?? '',
      managerName: json['managerName'] as String?,
      roleName: json['roleName'] as String? ?? '',
      dateOfJoining: json['dateOfJoining'] as String? ?? '',
      profileImageUrl: json['profileImageUrl'] as String?,
      emergencyContactName: json['emergencyContactName'] as String?,
      emergencyContactPhone: json['emergencyContactPhone'] as String?,
      emergencyContactRelation: json['emergencyContactRelation'] as String?,
    );
  }

  final String id;
  final String employeeCode;
  final String firstName;
  final String lastName;
  final String email;
  final String? phoneNumber;
  final String departmentName;
  final String designationTitle;
  final String locationName;
  final String? managerName;
  final String roleName;
  final String dateOfJoining;
  final String? profileImageUrl;
  final String? emergencyContactName;
  final String? emergencyContactPhone;
  final String? emergencyContactRelation;

  String get fullName => '$firstName $lastName';
}
