# Common.Tests Implementation Backlog

This backlog is ordered by priority and intended to be implemented top-to-bottom.

---

## 1. Datatypes

Value types, records, and enums for domain modeling across services.

### 1.1 SearchQuery construction and property access
- **Purpose:** Verify SearchQuery record can be created with all parameter combinations.
- **Suggested test name:** `SearchQuery_CanBeConstructedWithVariousParameters`
- **Success checks:**
  - Can construct with no parameters (all null).
  - Can construct with individual parameters.
  - Can construct with multiple parameters.
  - All properties are accessible and retain their values.
  - Null vs. non-null parameters are handled correctly.

### 1.2 SearchQuery immutability
- **Purpose:** Verify SearchQuery is immutable after construction.
- **Suggested test name:** `SearchQuery_IsImmutable`
- **Success checks:**
  - SearchQuery is a record (value type semantics).
  - Once created, properties cannot be modified.
  - Two identical SearchQuery instances are equal.
  - Equality comparison works correctly.

### 1.3 ComicInfo XML deserialization
- **Purpose:** Verify ComicInfo can be deserialized from XML.
- **Suggested test name:** `ComicInfo_CanBeDeserializedFromXml`
- **Success checks:**
  - Valid ComicInfo XML can be deserialized.
  - All properties are populated from XML.
  - Numeric and enum fields are parsed correctly.
  - Collections (Pages) are properly deserialized.

### 1.4 ComicInfo XML serialization
- **Purpose:** Verify ComicInfo can be serialized to XML.
- **Suggested test name:** `ComicInfo_CanBeSerializedToXml`
- **Success checks:**
  - ComicInfo instance can be serialized to XML.
  - XML contains all non-empty properties.
  - XML is valid and well-formed.
  - Round-trip serialization/deserialization preserves data.

### 1.5 ComicInfo default values
- **Purpose:** Verify default values are set correctly.
- **Suggested test name:** `ComicInfo_HasCorrectDefaultValues`
- **Success checks:**
  - String fields default to empty string.
  - Numeric fields have documented defaults (-1 for optional counts, 0 for PageCount).
  - Enum fields default to Unknown.
  - CommunityRating defaults are respected.

### 1.6 ComicInfoHelpers converts to SearchQuery
- **Purpose:** Verify ComicInfo can be converted to SearchQuery.
- **Suggested test name:** `ComicInfoHelpers_ConvertsToSearchQuery`
- **Success checks:**
  - `ToSearchQuery()` returns a valid SearchQuery.
  - Title is mapped from ComicInfo.Title.
  - Other fields are set correctly (currently only Title is mapped).
  - Null ComicInfo title is handled.

### 1.7 Language construction from culture name
- **Purpose:** Verify Language wraps CultureInfo correctly.
- **Suggested test name:** `Language_CanBeConstructedFromCultureName`
- **Success checks:**
  - Language can be created with valid culture names ("en", "en-US", "ja", etc.).
  - Language inherits CultureInfo properties.
  - ToString() returns the culture name.
  - Name property is accessible.

### 1.8 Language implicit operator to string
- **Purpose:** Verify Language implicitly converts to string.
- **Suggested test name:** `Language_ImplicitlyConvertsToString`
- **Success checks:**
  - Language instance can be implicitly cast to string.
  - Null Language converts to null string.
  - Resulting string is the culture name.

### 1.9 Language implicit operator from string
- **Purpose:** Verify string implicitly converts to Language.
- **Suggested test name:** `Language_ImplicitlyConvertsFromString`
- **Success checks:**
  - String can be implicitly cast to Language.
  - Null string converts to null Language.
  - Resulting Language has the correct culture name.

### 1.10 ReleaseStatus enum values
- **Purpose:** Verify ReleaseStatus enum has expected values.
- **Suggested test name:** `ReleaseStatus_HasExpectedValues`
- **Success checks:**
  - ReleaseStatus.Ongoing exists.
  - ReleaseStatus.Complete exists.
  - ReleaseStatus.Hiatus exists.
  - ReleaseStatus.Cancelled exists.

### 1.11 ReleaseStatusHelpers ParseStatus with valid inputs
- **Purpose:** Verify status parsing works for known strings.
- **Suggested test name:** `ReleaseStatusHelpers_ParsesValidStatusStrings`
- **Success checks:**
  - "ongoing" parses to ReleaseStatus.Ongoing.
  - "releasing" parses to ReleaseStatus.Ongoing.
  - "complete" parses to ReleaseStatus.Complete.
  - "completed" parses to ReleaseStatus.Complete.
  - "hiatus" parses to ReleaseStatus.Hiatus.
  - Parsing is case-insensitive.
  - Null input returns null.

### 1.12 ReleaseStatusHelpers ParseStatus with invalid inputs
- **Purpose:** Verify invalid status strings return null.
- **Suggested test name:** `ReleaseStatusHelpers_ReturnsNullForUnknownStatus`
- **Success checks:**
  - Unknown status string returns null.
  - Empty string returns null.
  - Whitespace-only string returns null.

### 1.13 ContentRating enum values
- **Purpose:** Verify ContentRating enum has expected values.
- **Suggested test name:** `ContentRating_HasExpectedValues`
- **Success checks:**
  - ContentRating.Safe exists.
  - ContentRating.Suggestive exists.
  - ContentRating.Erotica exists.
  - ContentRating.Pornographic exists.

### 1.14 ContentRatingExtensions TryParseContentRating with valid inputs
- **Purpose:** Verify content rating parsing works.
- **Suggested test name:** `ContentRatingExtensions_ParsesValidRatingStrings`
- **Success checks:**
  - "Safe" parses to ContentRating.Safe.
  - "Suggestive" parses to ContentRating.Suggestive.
  - "Erotica" parses to ContentRating.Erotica.
  - "Pornographic" parses to ContentRating.Pornographic.
  - Parsing is case-insensitive.

### 1.15 ContentRatingExtensions IsNsfw returns correct values
- **Purpose:** Verify NSFW classification is correct.
- **Suggested test name:** `ContentRatingExtensions_IsNsfw_ReturnsCorrectValues`
- **Success checks:**
  - ContentRating.Safe returns false.
  - ContentRating.Suggestive returns false.
  - ContentRating.Erotica returns true.
  - ContentRating.Pornographic returns true.

---

## 2. Helpers

Utility classes for HTTP requests, images, and URIs.

### 2.1 ClientSideRateLimitedHandler respects rate limit
- **Purpose:** Verify rate limiting is enforced on HTTP requests.
- **Suggested test name:** `ClientSideRateLimitedHandler_RespectsRateLimit`
- **Success checks:**
  - Requests are throttled according to the rate limiter.
  - No requests exceed the configured rate limit.
  - Multiple concurrent requests queue correctly.

### 2.2 ClientSideRateLimitedHandler acquires lease before sending
- **Purpose:** Verify a lease is acquired before each request.
- **Suggested test name:** `ClientSideRateLimitedHandler_AcquiresLeaseBeforeSending`
- **Success checks:**
  - Each request waits for a rate limit lease.
  - Request is sent only after lease is acquired.
  - Lease is properly released.

### 2.3 ClientSideRateLimitedHandler returns 429 when lease denied
- **Purpose:** Verify behavior when lease cannot be acquired.
- **Suggested test name:** `ClientSideRateLimitedHandler_Returns429OnLeaseDenied`
- **Success checks:**
  - If lease cannot be acquired, a 429 Too Many Requests response is returned.
  - Retry-After header is populated if available.

### 2.4 ClientSideRateLimitedHandler returns 408 on cancellation
- **Purpose:** Verify timeout behavior when cancellation is requested.
- **Suggested test name:** `ClientSideRateLimitedHandler_Returns408OnCancellation`
- **Success checks:**
  - If cancellation is requested while waiting for lease, 408 Request Timeout is returned.
  - No request is sent.

### 2.5 ClientSideRateLimitedHandler disposes limiter on disposal
- **Purpose:** Verify proper cleanup on disposal.
- **Suggested test name:** `ClientSideRateLimitedHandler_DisposesLimiterOnDisposal`
- **Success checks:**
  - When disposed, the rate limiter is disposed.
  - No lingering resources remain.

### 2.6 ImageHelper converts image to JPEG in-place
- **Purpose:** Verify image is converted to JPEG format.
- **Suggested test name:** `ImageHelper_ProcessConvertsToJpeg`
- **Success checks:**
  - `Process()` converts image to JPEG.
  - Result is JPEG-encoded.
  - Stream position is reset to 0.

### 2.7 ImageHelper ToJpeg conversion
- **Purpose:** Verify JPEG conversion method.
- **Suggested test name:** `ImageHelper_ToJpeg_ConvertsSuccessfully`
- **Success checks:**
  - `ToJpeg()` converts a TrangaImage to JPEG.
  - Result is valid JPEG data.
  - Stream position is reset after conversion.

### 2.8 ImageHelper AsJpeg returns new stream
- **Purpose:** Verify AsJpeg returns a separate stream.
- **Suggested test name:** `ImageHelper_AsJpeg_ReturnsNewStream`
- **Success checks:**
  - `AsJpeg()` returns a new MemoryStream.
  - Original stream is not modified.
  - Returned stream contains JPEG data.
  - Returned stream position is at 0.

### 2.9 ImageHelper handles various image formats
- **Purpose:** Verify support for multiple image formats.
- **Suggested test name:** `ImageHelper_HandlesMultipleImageFormats`
- **Success checks:**
  - PNG images are converted to JPEG.
  - BMP images are converted to JPEG.
  - GIF images are converted to JPEG.
  - WebP images are converted to JPEG (if supported).
  - Unsupported formats throw NotSupportedException or InvalidImageContentException.

### 2.10 RequestClient sets user agent
- **Purpose:** Verify User-Agent header is set.
- **Suggested test name:** `RequestClient_SetsUserAgentHeader`
- **Success checks:**
  - Default User-Agent is "Tranga/2.1".
  - User-Agent is included in all requests.
  - ProductInfoHeaderValue is properly formatted.

### 2.11 RequestClient constructor with rate limiter
- **Purpose:** Verify RequestClient can be created with rate limiting.
- **Suggested test name:** `RequestClient_ConstructorWithRateLimiter`
- **Success checks:**
  - RequestClient can be instantiated with a RateLimiter.
  - Rate limiter is used for subsequent requests.
  - HTTP handler chain includes ClientSideRateLimitedHandler.

### 2.12 RequestClient constructor without rate limiter
- **Purpose:** Verify RequestClient can be created without rate limiting.
- **Suggested test name:** `RequestClient_ConstructorWithoutRateLimiter`
- **Success checks:**
  - RequestClient can be instantiated with no arguments.
  - No rate limiter is applied.
  - HTTP handler chain still includes Cloudflare handler if configured.

### 2.13 RequestClient SendAsyncAndParseJson parses on success
- **Purpose:** Verify JSON parsing on successful response.
- **Suggested test name:** `RequestClient_SendAsyncAndParseJson_ParsesOnSuccess`
- **Success checks:**
  - If response is 2xx, JSON is parsed to the specified type.
  - Deserialization works correctly for complex objects.
  - Result is not null on valid JSON.

### 2.14 RequestClient SendAsyncAndParseJson returns null on error
- **Purpose:** Verify null is returned for non-success responses.
- **Suggested test name:** `RequestClient_SendAsyncAndParseJson_ReturnsNullOnError`
- **Success checks:**
  - If response is 4xx or 5xx, null is returned.
  - No exception is thrown.
  - JSON parsing is skipped.

### 2.15 UriHelper AddQueryParameter adds single parameter
- **Purpose:** Verify query parameter is added to URI.
- **Suggested test name:** `UriHelper_AddQueryParameter_AddsParameter`
- **Success checks:**
  - A query parameter is appended to the URI.
  - Parameter name and value are included.
  - Query string is properly formatted.

### 2.16 UriHelper AddQueryParameter chains multiple parameters
- **Purpose:** Verify multiple parameters can be chained.
- **Suggested test name:** `UriHelper_AddQueryParameter_ChainsMultipleParameters`
- **Success checks:**
  - Multiple `AddQueryParameter` calls chain correctly.
  - All parameters appear in the final query string.
  - Order is preserved.

### 2.17 UriHelper AddQueryParameter handles special characters
- **Purpose:** Verify special characters are handled correctly.
- **Suggested test name:** `UriHelper_AddQueryParameter_HandlesSpecialCharacters`
- **Success checks:**
  - Spaces and special characters are encoded.
  - Resulting URI is valid.

---

## 3. Settings

Configuration and environment variable management.

### 3.1 Constants directories defined
- **Purpose:** Verify directory path constants exist.
- **Suggested test name:** `Constants_HasRequiredDirectories`
- **Success checks:**
  - Constants.MangaDirectory == "Mangas".
  - Constants.CoverDirectory == "Covers".

### 3.2 Constants timeouts defined
- **Purpose:** Verify timeout constants are set.
- **Suggested test name:** `Constants_HasRequiredTimeouts`
- **Success checks:**
  - Constants.WorkerPickupWorkTimeout is 1 second.
  - Constants.SchedulerCreateWorkTimeout is 5 seconds.
  - Timeouts are reasonable for their use case.

### 3.3 Constants OpenApiDocumentationRun flag
- **Purpose:** Verify OpenAPI docs-only mode detection.
- **Suggested test name:** `Constants_OpenApiDocumentationRun_CorrectlyDetectesDocsMode`
- **Success checks:**
  - OpenApiDocumentationRun is true only when entry assembly is "GetDocument.Insider".
  - OpenApiDocumentationRun is false in normal runs.
  - Flag can be tested by checking entry assembly name.

### 3.4 EnvVars DBName with default
- **Purpose:** Verify database name environment variable handling.
- **Suggested test name:** `EnvVars_DBName_DefaultsToTranga`
- **Success checks:**
  - DBName defaults to "tranga" if not set.
  - DBName respects environment variable if set.

### 3.5 EnvVars POSTGRES variables
- **Purpose:** Verify PostgreSQL connection variables.
- **Suggested test name:** `EnvVars_PostgresVariables_HaveCorrectDefaults`
- **Success checks:**
  - POSTGRES_HOST defaults to "tranga-pg".
  - POSTGRES_USER defaults to "postgres".
  - POSTGRES_PASSWORD defaults to "postgres".
  - POSTGRES_PORT defaults to 5432.
  - All can be overridden by environment variables.

### 3.6 EnvVars connection timeouts
- **Purpose:** Verify connection timeout settings.
- **Suggested test name:** `EnvVars_ConnectionTimeouts_HaveCorrectDefaults`
- **Success checks:**
  - DBConnectionLifetime defaults to 60.
  - DBConnectionTimeout defaults to 30.
  - DBCommandTimeout defaults to 60.
  - Values are parsed as integers.

### 3.7 EnvVars WorkersCount calculation
- **Purpose:** Verify worker count is calculated correctly.
- **Suggested test name:** `EnvVars_WorkersCount_CalculatesCorrectly`
- **Success checks:**
  - WorkersCount defaults to ProcessorCount / 2, minimum 1.
  - If ProcessorCount is 1, WorkersCount is 1 (minimum enforced).
  - WorkersCount can be overridden by environment variable.

### 3.8 EnvVars optional services
- **Purpose:** Verify optional environment variables are nullable.
- **Suggested test name:** `EnvVars_OptionalServices_CanBeNull`
- **Success checks:**
  - FlareSolverrUrl is null if not set.
  - KomgaApiKey is null if not set.
  - Both can be set by environment variables.

### 3.9 Settings AllowNSFW with default
- **Purpose:** Verify NSFW setting default and override.
- **Suggested test name:** `Settings_AllowNSFW_DefaultsToFalse`
- **Success checks:**
  - AllowNSFW defaults to false.
  - AllowNSFW can be set via environment variable.
  - Getter/setter work correctly.

### 3.10 Settings DownloadLanguage with default
- **Purpose:** Verify download language setting.
- **Suggested test name:** `Settings_DownloadLanguage_DefaultsToEnglish`
- **Success checks:**
  - DownloadLanguage defaults to "en".
  - Can be set to other language codes.
  - DownloadLanguage is a Language instance.

### 3.11 Settings ChapterNamingScheme with default
- **Purpose:** Verify chapter naming scheme setting.
- **Suggested test name:** `Settings_ChapterNamingScheme_HasCorrectDefault`
- **Success checks:**
  - ChapterNamingScheme has a default pattern.
  - Default includes volume and chapter placeholders.
  - Can be overridden by environment variable.

### 3.12 Settings property updates
- **Purpose:** Verify settings can be updated at runtime.
- **Suggested test name:** `Settings_PropertiesCanBeUpdated`
- **Success checks:**
  - AllowNSFW can be changed after initialization.
  - DownloadLanguage can be changed after initialization.
  - ChapterNamingScheme can be changed after initialization.
  - Updates are reflected in getters.

---

## 4. Integration Tests

End-to-end tests combining multiple components.

### 4.1 Language and SearchQuery integration
- **Purpose:** Verify Language works correctly in SearchQuery.
- **Suggested test name:** `Integration_Language_WorksInSearchQuery`
- **Success checks:**
  - SearchQuery accepts Language as Language parameter (if supported).
  - Language implicit conversion works in SearchQuery context.

### 4.2 ComicInfo roundtrip with helpers
- **Purpose:** Verify ComicInfo XML roundtrip and conversion.
- **Suggested test name:** `Integration_ComicInfo_RoundtripAndConvert`
- **Success checks:**
  - ComicInfo is serialized to XML.
  - XML is deserialized back to ComicInfo.
  - `ToSearchQuery()` works on the deserialized instance.
  - Title is preserved through the conversion.

### 4.3 RequestClient with ImageHelper
- **Purpose:** Verify HTTP client can fetch and convert images.
- **Suggested test name:** `Integration_RequestClient_FetchesAndProcessesImages`
- **Success checks:**
  - RequestClient fetches image data.
  - ImageHelper converts the data to JPEG.
  - Result is valid JPEG.

### 4.4 Settings and EnvVars coherence
- **Purpose:** Verify Settings and EnvVars use consistent environment variables.
- **Suggested test name:** `Integration_SettingsAndEnvVars_AreCoherent`
- **Success checks:**
  - Environment variables set for both are respected.
  - Defaults are consistent.
  - No conflicting behaviors.

### 4.5 ReleaseStatus parsing and SearchQuery
- **Purpose:** Verify status parsing can be used in search context.
- **Suggested test name:** `Integration_ReleaseStatus_WorksWithSearchQuery`
- **Success checks:**
  - Status string can be parsed.
  - Parsed status can be used in application logic.

---

## Recommended implementation order
1. **Datatypes** (1.1–1.15)
   - Foundation: enums, records, and value types used everywhere.
   - Establish shared domain model contract across all services.
   
2. **Settings** (3.1–3.12)
   - High priority: configuration tested early for correctness.
   - Ensure environment variables are read correctly at startup.
   
3. **Helpers** (2.1–2.17)
   - Medium priority: utilities that depend on stable datatypes.
   - Support HTTP clients and image processing throughout the system.
   
4. **Integration Tests** (4.1–4.5)
   - Final polish: ensures all pieces work together in realistic scenarios.

## Cross-Service Test Patterns

### Record/Entity Immutability Pattern
Datatypes 1.1–1.2, 1.7–1.9 establish a pattern for record immutability testing. This pattern is repeated across all services (SearchQuery, Language, Library, Manga, Chapter, Metadata, DownloadLink). When testing records:
- Verify construction with required fields is possible.
- Verify equality semantics work correctly (two instances with same values are equal).
- Document any constraints (field length limits, required vs. optional fields).

### Parsing and Conversion Pattern
Datatypes 1.11–1.15 establish patterns for parsing and conversion (status parsing, NSFW classification). Similar patterns appear in all services for DTO conversions and database transformations. When implementing these tests:
- Test valid inputs exhaustively.
- Test invalid/edge cases (null, empty, whitespace, out-of-range).
- Document tie-breaker behavior for ambiguous cases.

## Notes

### Test fixtures and mocks
- **Mock HttpMessageHandler:** For RequestClient tests, use MockHttpMessageHandler to avoid real HTTP calls.
- **Mock RateLimiter:** Use in-memory or fake rate limiter for ClientSideRateLimitedHandler tests.
- **Test images:** Small sample PNG, BMP, GIF, WebP files for ImageHelper tests.
- **Environment variable isolation:** Reset environment after each test to avoid state leakage.

### XML data for ComicInfo tests
- Use representative ComicInfo XML samples matching the schema.
- Include edge cases: empty values, null fields, collections.

### Database configuration testing
- Test connection string assembly from EnvVars.
- Verify PostgreSQL-specific variable precedence (POSTGRES_* over DB*).

### Performance considerations
- ClientSideRateLimitedHandler tests should verify timing without causing excessive delays.
- Use time-mocking or configured rate limiters for deterministic tests.

