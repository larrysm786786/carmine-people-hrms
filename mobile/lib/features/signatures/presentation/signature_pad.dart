import 'dart:convert';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';

/// A minimal hand-drawn signature capture surface. Deliberately built with only
/// CustomPainter/RepaintBoundary (no signature-pad package) since this project has no
/// Flutter SDK available locally to vet a new dependency before it reaches CI.
class SignaturePad extends StatefulWidget {
  const SignaturePad({super.key, required this.controller});

  final SignaturePadController controller;

  @override
  State<SignaturePad> createState() => _SignaturePadState();
}

class SignaturePadController {
  final List<List<Offset>> strokes = [];
  final boundaryKey = GlobalKey();
  VoidCallback? _onChanged;

  bool get isEmpty => strokes.isEmpty;

  void clear() {
    strokes.clear();
    _onChanged?.call();
  }

  Future<String> exportPngBase64() async {
    final boundary = boundaryKey.currentContext!.findRenderObject() as RenderRepaintBoundary;
    final image = await boundary.toImage(pixelRatio: 2);
    final byteData = await image.toByteData(format: ui.ImageByteFormat.png);
    return base64Encode(byteData!.buffer.asUint8List());
  }
}

class _SignaturePadState extends State<SignaturePad> {
  @override
  void initState() {
    super.initState();
    widget.controller._onChanged = () => setState(() {});
  }

  void _addPoint(Offset point) {
    setState(() => widget.controller.strokes.last.add(point));
  }

  @override
  Widget build(BuildContext context) {
    return RepaintBoundary(
      key: widget.controller.boundaryKey,
      child: Container(
        color: Colors.white,
        child: GestureDetector(
          onPanStart: (details) => setState(() => widget.controller.strokes.add([details.localPosition])),
          onPanUpdate: (details) => _addPoint(details.localPosition),
          child: CustomPaint(
            painter: _SignaturePainter(widget.controller.strokes),
            child: const SizedBox.expand(),
          ),
        ),
      ),
    );
  }
}

class _SignaturePainter extends CustomPainter {
  _SignaturePainter(this.strokes);

  final List<List<Offset>> strokes;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.black
      ..strokeWidth = 3
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;

    for (final stroke in strokes) {
      for (var i = 0; i < stroke.length - 1; i++) {
        canvas.drawLine(stroke[i], stroke[i + 1], paint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _SignaturePainter oldDelegate) => true;
}
