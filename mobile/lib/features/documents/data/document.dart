class DocumentItem {
  const DocumentItem({
    required this.id,
    required this.fileName,
    required this.documentType,
    required this.createdAt,
  });

  factory DocumentItem.fromJson(Map<String, dynamic> json) {
    return DocumentItem(
      id: json['id'] as String,
      fileName: json['fileName'] as String? ?? 'Document',
      documentType: json['documentType']?.toString() ?? '',
      createdAt: json['createdAt'] as String? ?? '',
    );
  }

  final String id;
  final String fileName;
  final String documentType;
  final String createdAt;

  /// Best-effort extension for local temp-file naming; the download itself
  /// is served by content-type, this is only for a sensible file suffix.
  String get extension {
    final dot = fileName.lastIndexOf('.');
    return dot == -1 ? 'bin' : fileName.substring(dot + 1);
  }
}
