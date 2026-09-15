import 'package:dio/dio.dart';
import 'package:social_api_client/social_api_client.dart';

/// Production-ready factory for [SocialApiClient].
///
/// Source of truth lives in `sdk-assets/flutter/` and is copied into the
/// generated package by `scripts/generate-mobile.mjs` on every run, so it
/// survives `openapi-generator` regeneration (which owns everything else
/// under `packages/social_api_client/`).
class SocialApiClientFactory {
  /// Creates a configured [SocialApiClient] instance.
  ///
  /// Option A (recommended when the spec names the scheme `bearer`): uses the
  /// built-in `setBearerAuth` mechanism backed by [BearerAuthInterceptor].
  ///
  /// Option B (default here): a custom Dio interceptor for full control —
  /// resolves the token asynchronously on every request and works regardless
  /// of the security scheme name in the OpenAPI document.
  static SocialApiClient create({
    required String baseUrl,
    required Future<String?> Function() getToken,
    Duration connectTimeout = const Duration(seconds: 15),
    Duration receiveTimeout = const Duration(seconds: 15),
  }) {
    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: connectTimeout,
        receiveTimeout: receiveTimeout,
      ),
    );

    // Option B: dynamic Bearer injection on every request.
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await getToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
      ),
    );

    return SocialApiClient(dio: dio, basePathOverride: baseUrl);
  }
}
