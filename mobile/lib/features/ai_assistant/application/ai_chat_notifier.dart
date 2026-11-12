import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/ai_repository.dart';

class ChatMessage {
  const ChatMessage({required this.text, required this.fromUser});
  final String text;
  final bool fromUser;
}

class AiChatNotifier extends AsyncNotifier<List<ChatMessage>> {
  @override
  Future<List<ChatMessage>> build() async => [];

  Future<void> send(String message) async {
    final current = state.valueOrNull ?? [];
    state = AsyncData([...current, ChatMessage(text: message, fromUser: true)]);
    final result = await AsyncValue.guard(() async {
      final history = current
          .map((m) => {'role': m.fromUser ? 'user' : 'assistant', 'content': m.text})
          .toList();
      final reply = await ref.read(aiRepositoryProvider).sendMessage(message, history);
      return [
        ...current,
        ChatMessage(text: message, fromUser: true),
        ChatMessage(text: reply, fromUser: false),
      ];
    });
    state = result;
  }
}

final aiChatNotifierProvider =
    AsyncNotifierProvider<AiChatNotifier, List<ChatMessage>>(AiChatNotifier.new);
