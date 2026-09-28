import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/home_repository.dart';
import '../data/home_summary.dart';

class HomeSummaryNotifier extends AsyncNotifier<HomeSummary> {
  @override
  Future<HomeSummary> build() {
    return ref.watch(homeRepositoryProvider).fetchSummary();
  }
}

final homeSummaryNotifierProvider =
    AsyncNotifierProvider<HomeSummaryNotifier, HomeSummary>(HomeSummaryNotifier.new);
