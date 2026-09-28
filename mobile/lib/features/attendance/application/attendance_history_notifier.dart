import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/attendance_record.dart';
import '../data/attendance_repository.dart';

class AttendanceHistoryNotifier extends AsyncNotifier<PagedResult<AttendanceRecord>> {
  @override
  Future<PagedResult<AttendanceRecord>> build() {
    return ref.watch(attendanceRepositoryProvider).fetchHistory(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;

    final nextPage = await ref
        .read(attendanceRepositoryProvider)
        .fetchHistory(page: current.page + 1);

    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }
}

final attendanceHistoryNotifierProvider =
    AsyncNotifierProvider<AttendanceHistoryNotifier, PagedResult<AttendanceRecord>>(
  AttendanceHistoryNotifier.new,
);
