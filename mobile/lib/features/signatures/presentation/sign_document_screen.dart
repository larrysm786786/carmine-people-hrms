import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../application/signature_document_notifiers.dart';
import '../data/signature_document_models.dart';
import 'signature_pad.dart';

class SignDocumentScreen extends ConsumerStatefulWidget {
  const SignDocumentScreen({super.key, required this.document});

  final SignatureDocument document;

  @override
  ConsumerState<SignDocumentScreen> createState() => _SignDocumentScreenState();
}

class _SignDocumentScreenState extends ConsumerState<SignDocumentScreen> {
  final _padController = SignaturePadController();
  bool _isSubmitting = false;
  String? _error;

  Future<void> _submit() async {
    if (_padController.isEmpty) {
      setState(() => _error = 'Please draw your signature before submitting.');
      return;
    }
    setState(() {
      _isSubmitting = true;
      _error = null;
    });
    try {
      final base64Png = await _padController.exportPngBase64();
      await ref.read(signatureDocumentPendingNotifierProvider.notifier).sign(widget.document.id, base64Png);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.document.title)),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Text(widget.document.content, style: const TextStyle(height: 1.5)),
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: Row(
              children: [
                const Text('Sign below', style: TextStyle(fontWeight: FontWeight.w700)),
                const Spacer(),
                TextButton(onPressed: () => _padController.clear(), child: const Text('Clear')),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Container(
              height: 180,
              decoration: BoxDecoration(border: Border.all(color: AppColors.gray)),
              child: SignaturePad(controller: _padController),
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
              child: Text(_error!, style: const TextStyle(color: AppColors.carmine)),
            ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: FilledButton(
              onPressed: _isSubmitting ? null : _submit,
              child: _isSubmitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Submit signature'),
            ),
          ),
        ],
      ),
    );
  }
}
