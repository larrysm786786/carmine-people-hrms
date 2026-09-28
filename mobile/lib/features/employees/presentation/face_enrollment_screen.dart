import 'package:flutter/material.dart';

class FaceEnrollmentScreen extends StatelessWidget {
  const FaceEnrollmentScreen({super.key, required this.employeeId});

  final String employeeId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Face Enrollment')),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Face enrollment is coming soon — it requires an on-device face-recognition '
            'model that has not been integrated yet.',
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}
