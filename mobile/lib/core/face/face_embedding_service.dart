import 'dart:convert';
import 'dart:io';

import 'package:camera/camera.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Captures a still frame and base64-encodes it for the backend's face
/// matcher — embedding extraction/comparison happens server-side.
class FaceEmbeddingService {
  Future<String> captureAsBase64(CameraController controller) async {
    final file = await controller.takePicture();
    final bytes = await File(file.path).readAsBytes();
    return base64Encode(bytes);
  }
}

final faceEmbeddingServiceProvider = Provider<FaceEmbeddingService>((ref) {
  return FaceEmbeddingService();
});
