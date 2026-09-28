/// Backend serializes every DateTime as UTC but the JSON string carries no
/// "Z"/offset suffix (a known round-trip gap through SQL Server's datetime2
/// columns) — DateTime.parse would otherwise treat it as local time, which is
/// wrong by the local UTC offset (e.g. ~5.5h for IST). Every server timestamp
/// must go through this parser, never DateTime.parse directly.
DateTime? parseServerUtc(String? value) {
  if (value == null || value.isEmpty) return null;
  final hasOffset = value.endsWith('Z') || RegExp(r'[+-]\d{2}:\d{2}$').hasMatch(value);
  final parsed = DateTime.parse(hasOffset ? value : '${value}Z');
  return parsed.toLocal();
}

String formatServerTime(String? value, {String fallback = '-'}) {
  final local = parseServerUtc(value);
  if (local == null) return fallback;
  final hh = local.hour.toString().padLeft(2, '0');
  final mm = local.minute.toString().padLeft(2, '0');
  return '$hh:$mm';
}

String formatServerDateTime(String? value, {String fallback = '-'}) {
  final local = parseServerUtc(value);
  if (local == null) return fallback;
  final dd = local.day.toString().padLeft(2, '0');
  final mo = local.month.toString().padLeft(2, '0');
  final hh = local.hour.toString().padLeft(2, '0');
  final mi = local.minute.toString().padLeft(2, '0');
  return '$dd/$mo/${local.year} $hh:$mi';
}
