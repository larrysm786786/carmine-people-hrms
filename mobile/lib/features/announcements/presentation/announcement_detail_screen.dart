import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/announcements_notifier.dart';
import '../data/announcement.dart';
import '../data/announcement_comment.dart';

class AnnouncementDetailScreen extends ConsumerStatefulWidget {
  const AnnouncementDetailScreen({super.key, required this.announcement});

  final Announcement announcement;

  @override
  ConsumerState<AnnouncementDetailScreen> createState() => _AnnouncementDetailScreenState();
}

class _AnnouncementDetailScreenState extends ConsumerState<AnnouncementDetailScreen> {
  final _controller = TextEditingController();
  bool _sending = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final text = _controller.text.trim();
    if (text.isEmpty) return;
    setState(() => _sending = true);
    await ref.read(announcementCommentsNotifierProvider(widget.announcement.id).notifier).addComment(text);
    if (!mounted) return;
    setState(() => _sending = false);
    _controller.clear();
  }

  @override
  Widget build(BuildContext context) {
    final comments = ref.watch(announcementCommentsNotifierProvider(widget.announcement.id));

    return Scaffold(
      appBar: AppBar(title: const Text('Announcement')),
      body: Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                PremiumCard(
                  color: AppColors.carmine,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.announcement.title,
                        style: const TextStyle(color: AppColors.white, fontSize: 18, fontWeight: FontWeight.w800),
                      ),
                      const SizedBox(height: 8),
                      Text(widget.announcement.body, style: const TextStyle(color: AppColors.white)),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                const Text('Comments', style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700)),
                const SizedBox(height: 10),
                AsyncValueView<List<AnnouncementComment>>(
                  value: comments,
                  onRetry: () => ref.invalidate(announcementCommentsNotifierProvider(widget.announcement.id)),
                  data: (list) {
                    if (list.isEmpty) {
                      return const EmptyStateView(message: 'No comments yet. Be the first to comment.');
                    }
                    return Column(
                      children: [
                        for (final comment in list)
                          Padding(
                            padding: const EdgeInsets.only(bottom: 8),
                            child: PremiumCard(
                              padding: const EdgeInsets.all(12),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    comment.employeeName,
                                    style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700, fontSize: 12),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(comment.body, style: const TextStyle(color: AppColors.muted)),
                                ],
                              ),
                            ),
                          ),
                      ],
                    );
                  },
                ),
              ],
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _controller,
                      decoration: const InputDecoration(hintText: 'Add a comment...'),
                      onSubmitted: (_) => _send(),
                    ),
                  ),
                  IconButton(
                    icon: _sending
                        ? const SizedBox(
                            height: 18,
                            width: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.send, color: AppColors.carmine),
                    onPressed: _sending ? null : _send,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
