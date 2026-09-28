import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/route_paths.dart';

class OnboardingScreen extends StatelessWidget {
  const OnboardingScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.waving_hand_outlined, size: 48),
              const SizedBox(height: 16),
              const Text('Welcome to Carmine People'),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: () => context.go(RoutePaths.login),
                child: const Text('Get started'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
