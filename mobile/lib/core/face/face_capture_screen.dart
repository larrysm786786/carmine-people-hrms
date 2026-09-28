import 'package:camera/camera.dart';
import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'face_embedding_service.dart';
import 'liveness_detector.dart';

/// Returns a base64-encoded captured face image, or null if the user cancels
/// or navigates away before a live blink is detected.
Future<String?> showFaceCheckinDialog(BuildContext context) {
  return showDialog<String>(
    context: context,
    barrierDismissible: false,
    builder: (_) => const FaceCheckinDialog(),
  );
}

class FaceCheckinDialog extends StatefulWidget {
  const FaceCheckinDialog({super.key});

  @override
  State<FaceCheckinDialog> createState() => _FaceCheckinDialogState();
}

class _FaceCheckinDialogState extends State<FaceCheckinDialog> {
  CameraController? _controller;
  LivenessDetector? _detector;
  bool _capturing = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _init();
  }

  Future<void> _init() async {
    try {
      final cameras = await availableCameras();
      final front = cameras.firstWhere(
        (c) => c.lensDirection == CameraLensDirection.front,
        orElse: () => cameras.first,
      );
      final controller = CameraController(front, ResolutionPreset.medium, enableAudio: false);
      await controller.initialize();
      if (!mounted) {
        await controller.dispose();
        return;
      }
      final detector = LivenessDetector(controller);
      detector.blinkDetected.listen((_) => _onBlinkDetected());
      setState(() {
        _controller = controller;
        _detector = detector;
      });
      await detector.start();
    } catch (e) {
      if (mounted) setState(() => _error = 'Camera unavailable. Please try again.');
    }
  }

  Future<void> _onBlinkDetected() async {
    if (_capturing || _controller == null) return;
    _capturing = true;
    await _detector?.stop();
    try {
      final base64Image = await FaceEmbeddingService().captureAsBase64(_controller!);
      if (mounted) Navigator.of(context).pop(base64Image);
    } catch (_) {
      if (mounted) setState(() => _error = 'Capture failed. Please try again.');
      _capturing = false;
      await _detector?.start();
    }
  }

  @override
  void dispose() {
    // Unconditional cleanup: covers cancel, early pop, and successful capture
    // alike, so the camera image stream never outlives this dialog.
    _detector?.dispose();
    _controller?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (_error != null)
              Text(_error!, style: const TextStyle(color: AppColors.carmine))
            else if (_controller != null && _controller!.value.isInitialized)
              AspectRatio(
                aspectRatio: _controller!.value.aspectRatio,
                child: CameraPreview(_controller!),
              )
            else
              const SizedBox(
                height: 200,
                child: Center(child: CircularProgressIndicator()),
              ),
            const SizedBox(height: 12),
            const Text('Blink to check in'),
            const SizedBox(height: 12),
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('Cancel'),
            ),
          ],
        ),
      ),
    );
  }
}
