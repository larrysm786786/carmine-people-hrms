import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/celebration.dart';
import '../data/employees_repository.dart';

class CelebrationsNotifier extends AsyncNotifier<List<Celebration>> {
  @override
  Future<List<Celebration>> build() {
    return ref.watch(employeesRepositoryProvider).fetchUpcomingCelebrations();
  }
}

final celebrationsNotifierProvider =
    AsyncNotifierProvider<CelebrationsNotifier, List<Celebration>>(CelebrationsNotifier.new);
