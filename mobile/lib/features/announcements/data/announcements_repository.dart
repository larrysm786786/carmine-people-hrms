import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'announcement.dart';
import 'announcement_comment.dart';

class AnnouncementsRepository {
  AnnouncementsRepository(this._client);

  final ApiClient _client;

  Future<List<Announcement>> fetchAll() async {
    final items = await _client.getList('/announcements');
    return items.map((e) => Announcement.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<AnnouncementComment>> fetchComments(String announcementId) async {
    final items = await _client.getList('/announcements/$announcementId/comments');
    return items.map((e) => AnnouncementComment.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> addComment(String announcementId, String body) {
    return _client.post('/announcements/$announcementId/comments', data: {'body': body});
  }
}

final announcementsRepositoryProvider = Provider<AnnouncementsRepository>((ref) {
  return AnnouncementsRepository(ref.watch(apiClientProvider));
});
