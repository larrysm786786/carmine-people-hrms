enum ShiftSwapStatus { requestedAcceptance, pending, approved, rejected, cancelled }

ShiftSwapStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= ShiftSwapStatus.values.length) return ShiftSwapStatus.requestedAcceptance;
  return ShiftSwapStatus.values[index];
}

extension ShiftSwapStatusLabel on ShiftSwapStatus {
  String get label {
    switch (this) {
      case ShiftSwapStatus.requestedAcceptance:
        return 'Awaiting acceptance';
      case ShiftSwapStatus.pending:
        return 'Awaiting manager';
      case ShiftSwapStatus.approved:
        return 'Approved';
      case ShiftSwapStatus.rejected:
        return 'Rejected';
      case ShiftSwapStatus.cancelled:
        return 'Cancelled';
    }
  }
}

class ShiftSwapItem {
  const ShiftSwapItem({
    required this.id,
    required this.requesterId,
    required this.requesterName,
    required this.counterpartId,
    required this.counterpartName,
    required this.requesterDate,
    required this.counterpartDate,
    this.reason,
    required this.status,
  });

  factory ShiftSwapItem.fromJson(Map<String, dynamic> json) {
    return ShiftSwapItem(
      id: json['id'] as String,
      requesterId: json['requesterId'] as String? ?? '',
      requesterName: json['requesterName'] as String? ?? '',
      counterpartId: json['counterpartId'] as String? ?? '',
      counterpartName: json['counterpartName'] as String? ?? '',
      requesterDate: json['requesterDate'] as String,
      counterpartDate: json['counterpartDate'] as String,
      reason: json['reason'] as String?,
      status: _statusFromJson(json['status']),
    );
  }

  final String id;
  final String requesterId;
  final String requesterName;
  final String counterpartId;
  final String counterpartName;
  final String requesterDate;
  final String counterpartDate;
  final String? reason;
  final ShiftSwapStatus status;
}
