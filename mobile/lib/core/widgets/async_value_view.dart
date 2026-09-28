import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'error_state_view.dart';
import 'friendly_error.dart';
import 'skeleton_loader.dart';

/// Every data-backed screen must render its primary AsyncValue through this
/// widget instead of hand-rolling loading/error branches — this is what makes
/// a missing error/retry path structurally impossible for new screens.
class AsyncValueView<T> extends StatelessWidget {
  const AsyncValueView({
    super.key,
    required this.value,
    required this.data,
    this.onRetry,
    this.loading,
    this.error,
  });

  final AsyncValue<T> value;
  final Widget Function(T data) data;
  final VoidCallback? onRetry;
  final WidgetBuilder? loading;
  final Widget Function(Object error, VoidCallback? retry)? error;

  @override
  Widget build(BuildContext context) {
    return value.when(
      data: data,
      loading: () => loading?.call(context) ?? const SkeletonLoader(),
      error: (err, _) {
        if (error != null) return error!(err, onRetry);
        return ErrorStateView(
          message: friendlyErrorMessage(err),
          onRetry: onRetry ?? () {},
        );
      },
    );
  }
}
