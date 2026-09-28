class OrgChartNode {
  const OrgChartNode({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.designationTitle,
    this.profileImageUrl,
    required this.reports,
  });

  factory OrgChartNode.fromJson(Map<String, dynamic> json) {
    final reportsJson = json['reports'] as List<dynamic>? ?? const [];
    return OrgChartNode(
      id: json['id'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      designationTitle: json['designationTitle'] as String? ?? '',
      profileImageUrl: json['profileImageUrl'] as String?,
      reports: reportsJson.map((e) => OrgChartNode.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }

  final String id;
  final String firstName;
  final String lastName;
  final String designationTitle;
  final String? profileImageUrl;
  final List<OrgChartNode> reports;

  String get fullName => '$firstName $lastName';
}
