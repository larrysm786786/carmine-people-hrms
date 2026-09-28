import '../network/api_client.dart';

class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.total,
  });

  factory PagedResult.fromRawPage(RawPage raw, T Function(Map<String, dynamic>) fromJson) {
    return PagedResult(
      items: raw.items.map((e) => fromJson(e as Map<String, dynamic>)).toList(),
      page: raw.page,
      pageSize: raw.pageSize,
      total: raw.total,
    );
  }

  final List<T> items;
  final int page;
  final int pageSize;
  final int total;

  bool get hasNextPage => page * pageSize < total;
}
