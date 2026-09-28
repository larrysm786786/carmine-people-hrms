import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Single flat theme built from exactly 4 colors (AppColors) — no seed-based
/// tint generation, no gradients, no elevation-based surface tinting.
class AppTheme {
  static ThemeData light() {
    const scheme = ColorScheme(
      brightness: Brightness.light,
      primary: AppColors.carmine,
      onPrimary: AppColors.white,
      secondary: AppColors.carmine,
      onSecondary: AppColors.white,
      error: AppColors.carmine,
      onError: AppColors.white,
      surface: AppColors.white,
      onSurface: AppColors.muted,
      surfaceContainerHighest: AppColors.gray,
      onSurfaceVariant: AppColors.muted,
      outline: AppColors.muted,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: AppColors.gray,
      canvasColor: AppColors.white,
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.white,
        foregroundColor: AppColors.muted,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
      ),
      cardTheme: const CardThemeData(
        color: AppColors.white,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.carmine,
          foregroundColor: AppColors.white,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.carmine,
          side: const BorderSide(color: AppColors.carmine),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AppColors.carmine),
      ),
      chipTheme: const ChipThemeData(
        backgroundColor: AppColors.gray,
        labelStyle: TextStyle(color: AppColors.muted),
        side: BorderSide(color: AppColors.muted),
      ),
      inputDecorationTheme: const InputDecorationTheme(
        filled: true,
        fillColor: AppColors.white,
        border: OutlineInputBorder(borderSide: BorderSide(color: AppColors.muted)),
        enabledBorder: OutlineInputBorder(borderSide: BorderSide(color: AppColors.muted)),
        focusedBorder: OutlineInputBorder(borderSide: BorderSide(color: AppColors.carmine, width: 2)),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AppColors.white,
        surfaceTintColor: Colors.transparent,
        indicatorColor: AppColors.gray,
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => TextStyle(
            color: states.contains(WidgetState.selected) ? AppColors.carmine : AppColors.muted,
          ),
        ),
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            color: states.contains(WidgetState.selected) ? AppColors.carmine : AppColors.muted,
          ),
        ),
      ),
      dividerTheme: const DividerThemeData(color: AppColors.gray),
      progressIndicatorTheme: const ProgressIndicatorThemeData(color: AppColors.carmine),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.carmine,
        foregroundColor: AppColors.white,
      ),
    );
  }

  /// This app uses one flat light theme only — no separate dark palette,
  /// since the 4-color set was specified without a dark variant.
  static ThemeData dark() => light();
}
