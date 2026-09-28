import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_mlkit_text_recognition/google_mlkit_text_recognition.dart';

import '../../../core/theme/app_colors.dart';
import '../application/documents_notifier.dart';

class IdProofUploadScreen extends ConsumerStatefulWidget {
  const IdProofUploadScreen({super.key});

  @override
  ConsumerState<IdProofUploadScreen> createState() => _IdProofUploadScreenState();
}

class _IdProofUploadScreenState extends ConsumerState<IdProofUploadScreen> {
  String? _filePath;
  String? _extractedText;
  bool _busy = false;
  String? _error;

  Future<void> _pickAndScan() async {
    setState(() {
      _busy = true;
      _error = null;
      _extractedText = null;
    });
    try {
      final result = await FilePicker.pickFile(type: FileType.image);
      final path = result?.path;
      if (path == null) {
        setState(() => _busy = false);
        return;
      }
      final recognizer = TextRecognizer(script: TextRecognitionScript.latin);
      final inputImage = InputImage.fromFilePath(path);
      final recognized = await recognizer.processImage(inputImage);
      await recognizer.close();
      setState(() {
        _filePath = path;
        _extractedText = recognized.text.isEmpty ? '(No text detected)' : recognized.text;
      });
    } catch (_) {
      setState(() => _error = 'Failed to scan the document. Please try again.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _confirmUpload() async {
    final path = _filePath;
    if (path == null) return;
    setState(() => _busy = true);
    final success = await ref.read(documentsNotifierProvider.notifier).uploadIdProof(path);
    if (!mounted) return;
    setState(() => _busy = false);
    if (success) {
      Navigator.of(context).pop();
    } else {
      setState(() => _error = 'Upload failed. Please try again.');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Upload ID Proof')),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_filePath != null)
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.file(File(_filePath!), height: 200, fit: BoxFit.cover),
                ),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: _busy ? null : _pickAndScan,
                icon: const Icon(Icons.image_search_outlined),
                label: const Text('Pick & Scan ID Document'),
              ),
              if (_extractedText != null) ...[
                const SizedBox(height: 16),
                const Text('Extracted text (please verify):'),
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppColors.gray,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_extractedText!),
                ),
              ],
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(_error!, style: const TextStyle(color: AppColors.carmine)),
                ),
              const SizedBox(height: 24),
              if (_filePath != null)
                FilledButton(
                  onPressed: _busy ? null : _confirmUpload,
                  child: _busy
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.white),
                        )
                      : const Text('Confirm & Upload'),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
