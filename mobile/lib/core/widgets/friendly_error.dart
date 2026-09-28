import '../attendance_support/wifi_checkin_service.dart';
import '../network/api_exception.dart';

String friendlyErrorMessage(Object error) {
  if (error is ApiException) return error.message;
  if (error is LocationUnavailableException) return error.message;
  return 'Something went wrong. Please try again.';
}
