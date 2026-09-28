import 'dart:async';
import 'dart:ui' as ui;

import 'package:camera/camera.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

/// Streams camera frames through ML Kit face detection to gate a check-in
/// capture on a live, blinking face. The image-stream subscription MUST be
/// cancelled unconditionally in stop()/dispose() — including when the caller
/// navigates away mid-capture — otherwise the camera plugin leaks a native
/// stream handler across screen instances.
class LivenessDetector {
  LivenessDetector(this._camera)
      : _faceDetector = FaceDetector(
          options: FaceDetectorOptions(
            enableClassification: true,
            performanceMode: FaceDetectorMode.fast,
          ),
        );

  final CameraController _camera;
  final FaceDetector _faceDetector;
  bool _processing = false;
  bool _started = false;

  final _blinkController = StreamController<bool>.broadcast();
  Stream<bool> get blinkDetected => _blinkController.stream;

  Future<void> start() async {
    if (_started) return;
    _started = true;
    await _camera.startImageStream(_onFrame);
  }

  void _onFrame(CameraImage image) {
    if (_processing) return;
    _processing = true;
    _detect(image).whenComplete(() => _processing = false);
  }

  Future<void> _detect(CameraImage image) async {
    try {
      final inputImage = _toInputImage(image);
      if (inputImage == null) return;
      final faces = await _faceDetector.processImage(inputImage);
      if (faces.isEmpty) return;
      final face = faces.first;
      final leftOpen = face.leftEyeOpenProbability ?? 1.0;
      final rightOpen = face.rightEyeOpenProbability ?? 1.0;
      if (leftOpen < 0.3 && rightOpen < 0.3) {
        if (!_blinkController.isClosed) _blinkController.add(true);
      }
    } catch (_) {
      // A single bad frame must never crash the capture flow.
    }
  }

  InputImage? _toInputImage(CameraImage image) {
    try {
      return InputImage.fromBytes(
        bytes: image.planes.first.bytes,
        metadata: InputImageMetadata(
          size: ui.Size(image.width.toDouble(), image.height.toDouble()),
          rotation: InputImageRotation.rotation0deg,
          format: InputImageFormat.nv21,
          bytesPerRow: image.planes.first.bytesPerRow,
        ),
      );
    } catch (_) {
      return null;
    }
  }

  Future<void> stop() async {
    if (!_started) return;
    _started = false;
    if (_camera.value.isStreamingImages) {
      await _camera.stopImageStream();
    }
  }

  Future<void> dispose() async {
    await stop();
    await _blinkController.close();
    await _faceDetector.close();
  }
}
