/// Names temp download files by documentId (not a scheme like sequential
/// counters or original-filename reuse, which can collide across repeated
/// downloads of different documents in the same session).
class UniqueFileNamer {
  static String forDocument(String documentId, String extension) {
    final cleanExt = extension.startsWith('.') ? extension.substring(1) : extension;
    return '$documentId.$cleanExt';
  }
}
