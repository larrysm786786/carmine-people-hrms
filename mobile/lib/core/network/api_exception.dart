sealed class ApiException implements Exception {
  const ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}

class NetworkException extends ApiException {
  const NetworkException([super.message = 'No internet connection.']);
}

class TimeoutApiException extends ApiException {
  const TimeoutApiException([super.message = 'Request timed out. Please try again.']);
}

class ServerException extends ApiException {
  const ServerException(this.statusCode, super.message, {this.body});

  final int statusCode;
  final Map<String, dynamic>? body;
}

class UnauthorizedException extends ServerException {
  const UnauthorizedException(super.statusCode, super.message, {super.body});
}

class UnknownApiException extends ApiException {
  const UnknownApiException([super.message = 'Something went wrong. Please try again.']);
}
