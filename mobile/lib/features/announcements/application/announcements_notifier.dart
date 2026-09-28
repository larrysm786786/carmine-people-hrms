import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/announcement.dart';
import '../data/announcement_comment.dart';
import '../data/announcements_repository.dart';

class AnnouncementsNotifier extends AsyncNotifier<List<Announcement>> {
  @override
  Future<List<Announcement>> build() {
    return ref.watch(announcementsRepositoryProvider).fetchAll();
  }
}

final announcementsNotifierProvider =
    AsyncNotifierProvider<AnnouncementsNotifier, List<Announcement>>(AnnouncementsNotifier.new);

class AnnouncementCommentsNotifier extends FamilyAsyncNotifier<List<AnnouncementComment>, String> {
  @override
  Future<List<AnnouncementComment>> build(String announcementId) {
    return ref.watch(announcementsRepositoryProvider).fetchComments(announcementId);
  }

  Future<void> addComment(String body) async {
    await ref.read(announcementsRepositoryProvider).addComment(arg, body);
    ref.invalidateSelf();
  }
}

final announcementCommentsNotifierProvider =
    AsyncNotifierProviderFamily<AnnouncementCommentsNotifier, List<AnnouncementComment>, String>(
  AnnouncementCommentsNotifier.new,
);
