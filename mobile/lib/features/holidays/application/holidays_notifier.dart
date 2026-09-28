import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/holiday.dart';
import '../data/holidays_repository.dart';

class HolidaysNotifier extends AsyncNotifier<List<Holiday>> {
  @override
  Future<List<Holiday>> build() {
    return ref.watch(holidaysRepositoryProvider).fetchAll();
  }
}

final holidaysNotifierProvider =
    AsyncNotifierProvider<HolidaysNotifier, List<Holiday>>(HolidaysNotifier.new);
