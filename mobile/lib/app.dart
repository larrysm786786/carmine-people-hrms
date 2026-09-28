import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/auth_session/auth_notifier.dart';
import 'core/router/app_router.dart';
import 'core/storage/token_storage.dart';
import 'core/theme/app_theme.dart';

class CarminePeopleApp extends ConsumerStatefulWidget {
  const CarminePeopleApp({super.key});

  @override
  ConsumerState<CarminePeopleApp> createState() => _CarminePeopleAppState();
}

class _CarminePeopleAppState extends ConsumerState<CarminePeopleApp> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Lock on backgrounding, not on every pause — a brief pause for a system
    // dialog (permission prompt, camera) must not force a re-unlock, only an
    // actual switch away from the app should.
    if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      ref.read(tokenStorageProvider).readBiometricEnabled().then((enabled) {
        if (enabled) ref.read(authNotifierProvider.notifier).lock();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(appRouterProvider);

    return MaterialApp.router(
      title: 'Carmine People',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      routerConfig: router,
    );
  }
}
