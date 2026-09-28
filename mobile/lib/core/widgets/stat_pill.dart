import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// A small flat metric block used to show a single number + label, e.g. on
/// the Home dashboard's monthly summary row. No gradient, no shading —
/// carmine number on a flat gray chip.
class StatPill extends StatelessWidget {
  const StatPill({super.key, required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    // No Expanded/Flexible here — this widget must work equally inside a
    // Row (home_screen.dart's monthly-summary strip) and inside a Wrap
    // (leave_screen.dart's balances grid, via a fixed-width SizedBox), and
    // Expanded throws when its parent isn't a Flex. Callers that want it to
    // fill a Row slot wrap it in their own Expanded instead.
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.gray,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            value,
            style: const TextStyle(
              color: AppColors.carmine,
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
        ],
      ),
    );
  }
}
