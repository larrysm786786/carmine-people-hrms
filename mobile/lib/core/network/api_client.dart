import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/token_storage.dart';
import 'api_config.dart';
import 'api_exception.dart';

class RawPage {
  const RawPage({required this.items, required this.page, required this.pageSize, required this.total});

  final List<dynamic> items;
  final int page;
  final int pageSize;
  final int total;

  bool get hasNextPage => page * pageSize < total;
}

/// Single shared Dio-backed client. Every feature repository must go through
/// this — no feature is permitted to instantiate its own Dio instance.
class ApiClient {
  ApiClient(this._apiConfig, this._tokenStorage) {
    _dio = Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        sendTimeout: const Duration(seconds: 15),
        // ngrok's free tunnel shows an HTML "visitor warning" interstitial to
        // any client that doesn't send this header, which the app would
        // otherwise misread as a malformed/non-JSON response (surfacing as
        // "no internet"). Harmless to send against any other backend host.
        headers: {'ngrok-skip-browser-warning': 'true'},
      ),
    );
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: _onRequest,
        onError: _onError,
      ),
    );
  }

  late final Dio _dio;
  final ApiConfig _apiConfig;
  final TokenStorage _tokenStorage;

  Future<void>? _refreshInFlight;

  Future<void> _onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    options.baseUrl = await _apiConfig.resolveBaseUrl();
    if (!options.path.contains('/auth/login') && !options.path.contains('/auth/refresh')) {
      final token = await _tokenStorage.readAccessToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }

  Future<void> _onError(DioException err, ErrorInterceptorHandler handler) async {
    final isUnauthorized = err.response?.statusCode == 401;
    final isAuthEndpoint = err.requestOptions.path.contains('/auth/');

    if (isUnauthorized && !isAuthEndpoint) {
      try {
        await _refreshTokenDeduped();
        final retryResponse = await _dio.fetch(err.requestOptions);
        return handler.resolve(retryResponse);
      } catch (_) {
        // Refresh failed: fall through to the original error so the caller's
        // AsyncValue.guard surfaces it as Unauthorized, not a silent hang.
      }
    }
    handler.next(err);
  }

  Future<void> _refreshTokenDeduped() {
    return _refreshInFlight ??= _performRefresh().whenComplete(() {
      _refreshInFlight = null;
    });
  }

  Future<void> _performRefresh() async {
    final refreshToken = await _tokenStorage.readRefreshToken();
    if (refreshToken == null) {
      throw const UnauthorizedException(401, 'No refresh token available.');
    }
    final baseUrl = await _apiConfig.resolveBaseUrl();
    try {
      final response = await Dio(BaseOptions(
        baseUrl: baseUrl,
        headers: {'ngrok-skip-browser-warning': 'true'},
      )).post(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );
      final data = response.data['data'] as Map<String, dynamic>;
      await _tokenStorage.saveTokens(
        accessToken: data['accessToken'] as String,
        refreshToken: data['refreshToken'] as String,
      );
    } on DioException {
      // Only a definitive server-confirmed rejection wipes tokens; any other
      // failure (timeout, no connectivity) must leave stored tokens intact so
      // a transient network blip doesn't force a full re-login.
      rethrow;
    }
  }

  Future<Map<String, dynamic>?> get(String path, {Map<String, dynamic>? queryParameters}) {
    return _request(() => _dio.get(path, queryParameters: queryParameters));
  }

  Future<Map<String, dynamic>?> post(String path, {Object? data}) {
    return _request(() => _dio.post(path, data: data));
  }

  Future<Map<String, dynamic>?> put(String path, {Object? data, Map<String, dynamic>? queryParameters}) {
    return _request(() => _dio.put(path, data: data, queryParameters: queryParameters));
  }

  Future<Map<String, dynamic>?> delete(String path, {Object? data}) {
    return _request(() => _dio.delete(path, data: data));
  }

  Future<void> downloadToFile(String path, String savePath) async {
    try {
      await _dio.download(path, savePath);
    } on DioException catch (e) {
      throw _mapDioException(e);
    }
  }

  Future<Map<String, dynamic>?> postMultipart(
    String path,
    String filePath, {
    String fileField = 'file',
    Map<String, dynamic>? fields,
  }) {
    return _request(() async {
      final formData = FormData.fromMap({
        ...?fields,
        fileField: await MultipartFile.fromFile(filePath),
      });
      return _dio.post(path, data: formData);
    });
  }

  Future<Map<String, dynamic>> _requestEnvelope(Future<Response> Function() call) async {
    try {
      final response = await call();
      final body = response.data;
      if (body is Map<String, dynamic>) {
        if (body['success'] == false) {
          final error = body['error'] as Map<String, dynamic>?;
          throw ServerException(
            response.statusCode ?? 500,
            error?['message'] as String? ?? 'Request failed.',
            body: body,
          );
        }
        return body;
      }
      return const {};
    } on DioException catch (e) {
      throw _mapDioException(e);
    }
  }

  Future<Map<String, dynamic>?> _request(Future<Response> Function() call) async {
    final envelope = await _requestEnvelope(call);
    return envelope['data'] as Map<String, dynamic>?;
  }

  /// For endpoints whose "data" field is a bare JSON array (e.g. GET
  /// /announcements, GET /holidays, GET /leaves/types).
  Future<List<dynamic>> getList(String path, {Map<String, dynamic>? queryParameters}) async {
    final envelope = await _requestEnvelope(() => _dio.get(path, queryParameters: queryParameters));
    return envelope['data'] as List<dynamic>? ?? const [];
  }

  /// For paged endpoints shaped as {data: [...], meta: {page, pageSize, total}}.
  Future<RawPage> getPage(String path, {Map<String, dynamic>? queryParameters}) async {
    final envelope = await _requestEnvelope(() => _dio.get(path, queryParameters: queryParameters));
    final items = envelope['data'] as List<dynamic>? ?? const [];
    final meta = envelope['meta'] as Map<String, dynamic>? ?? const {};
    return RawPage(
      items: items,
      page: meta['page'] as int? ?? 1,
      pageSize: meta['pageSize'] as int? ?? items.length,
      total: meta['total'] as int? ?? items.length,
    );
  }

  ApiException _mapDioException(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const TimeoutApiException();
      case DioExceptionType.connectionError:
        return const NetworkException();
      case DioExceptionType.badResponse:
        final statusCode = e.response?.statusCode ?? 500;
        final body = e.response?.data is Map<String, dynamic>
            ? e.response?.data as Map<String, dynamic>
            : null;
        final message = body?['error']?['message'] as String? ?? 'Server error.';
        if (statusCode == 401) {
          return UnauthorizedException(statusCode, message, body: body);
        }
        return ServerException(statusCode, message, body: body);
      default:
        return const UnknownApiException();
    }
  }
}

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(ref.watch(apiConfigProvider), ref.watch(tokenStorageProvider));
});
