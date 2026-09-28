class Payslip {
  const Payslip({
    required this.id,
    required this.year,
    required this.month,
    required this.grossAmount,
    required this.deductionsAmount,
    required this.netAmount,
    this.notes,
  });

  factory Payslip.fromJson(Map<String, dynamic> json) {
    return Payslip(
      id: json['id'] as String,
      year: json['year'] as int,
      month: json['month'] as int,
      grossAmount: (json['grossAmount'] as num).toDouble(),
      deductionsAmount: (json['deductionsAmount'] as num).toDouble(),
      netAmount: (json['netAmount'] as num).toDouble(),
      notes: json['notes'] as String?,
    );
  }

  final String id;
  final int year;
  final int month;
  final double grossAmount;
  final double deductionsAmount;
  final double netAmount;
  final String? notes;

  static const _monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];

  String get monthLabel => '${_monthNames[month - 1]} $year';
}
