enum ExpenseStatus { pending, approved, rejected, reimbursed }

ExpenseStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= ExpenseStatus.values.length) {
    return ExpenseStatus.pending;
  }
  return ExpenseStatus.values[index];
}

extension ExpenseStatusLabel on ExpenseStatus {
  String get label {
    switch (this) {
      case ExpenseStatus.pending:
        return 'Pending';
      case ExpenseStatus.approved:
        return 'Approved';
      case ExpenseStatus.rejected:
        return 'Rejected';
      case ExpenseStatus.reimbursed:
        return 'Reimbursed';
    }
  }
}

class Expense {
  const Expense({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.category,
    required this.amount,
    required this.expenseDate,
    required this.description,
    required this.status,
  });

  factory Expense.fromJson(Map<String, dynamic> json) {
    return Expense(
      id: json['id'] as String,
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      category: json['category'] as String,
      amount: (json['amount'] as num).toDouble(),
      expenseDate: json['expenseDate'] as String,
      description: json['description'] as String?,
      status: _statusFromJson(json['status']),
    );
  }

  final String id;
  final String employeeId;
  final String employeeName;
  final String category;
  final double amount;
  final String expenseDate;
  final String? description;
  final ExpenseStatus status;
}
