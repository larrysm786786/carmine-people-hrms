import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/org_chart_notifier.dart';
import '../data/org_chart_node.dart';

class OrgChartScreen extends ConsumerWidget {
  const OrgChartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final chart = ref.watch(orgChartNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Org Chart')),
      body: AsyncValueView<List<OrgChartNode>>(
        value: chart,
        onRetry: () => ref.invalidate(orgChartNotifierProvider),
        data: (roots) {
          if (roots.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No org chart data available.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final root in roots) _OrgChartTile(node: root, depth: 0),
            ],
          );
        },
      ),
    );
  }
}

class _OrgChartTile extends StatefulWidget {
  const _OrgChartTile({required this.node, required this.depth});

  final OrgChartNode node;
  final int depth;

  @override
  State<_OrgChartTile> createState() => _OrgChartTileState();
}

class _OrgChartTileState extends State<_OrgChartTile> {
  bool _expanded = true;

  @override
  Widget build(BuildContext context) {
    final hasReports = widget.node.reports.isNotEmpty;

    return Padding(
      padding: EdgeInsets.only(left: widget.depth * 20.0, bottom: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          PremiumCard(
            padding: const EdgeInsets.all(12),
            onTap: hasReports ? () => setState(() => _expanded = !_expanded) : null,
            child: Row(
              children: [
                Container(
                  width: 36,
                  height: 36,
                  decoration: const BoxDecoration(color: AppColors.gray, shape: BoxShape.circle),
                  child: const Icon(Icons.person_outline, color: AppColors.carmine, size: 18),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.node.fullName,
                        style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                      ),
                      Text(
                        widget.node.designationTitle,
                        style: const TextStyle(color: AppColors.muted, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                if (hasReports)
                  Icon(
                    _expanded ? Icons.expand_less : Icons.expand_more,
                    color: AppColors.muted,
                  ),
              ],
            ),
          ),
          if (_expanded)
            for (final report in widget.node.reports)
              _OrgChartTile(node: report, depth: widget.depth + 1),
        ],
      ),
    );
  }
}
