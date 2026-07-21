# Services.Notifications.Tests Implementation Backlog

This backlog is ordered by priority and intended to be implemented top-to-bottom.

**See also:** `SHARED_TEST_PATTERNS.md` for reusable test patterns used across all services.

---

## 1. Sending Notifications

Core logic for dispatching notifications to configured extensions.

### 1.1 SendNotifications broadcasts to all configured extensions
- **Purpose:** Verify notifications are sent to all registered extensions.
- **Suggested test name:** `SendNotifications_BroadcastsToAllConfiguredExtensions`
- **Success checks:**
  - Given multiple extensions are configured.
  - When a notification is sent.
  - Then each extension's `SendNotification` method is called exactly once.
  - And all tasks complete successfully.

### 1.2 SendNotifications returns true on successful dispatch
- **Purpose:** Verify success indicator is returned when all sends complete.
- **Suggested test name:** `SendNotifications_ReturnsTrueOnSuccessfulDispatch`
- **Success checks:**
  - Given at least one extension is configured.
  - When `SendNotifications` is called.
  - Then the result is `true`.

### 1.3 SendNotifications returns false when no extensions exist
- **Purpose:** Verify behavior when there are no configured extensions.
- **Suggested test name:** `SendNotifications_ReturnsFalseWhenNoExtensionsExist`
- **Success checks:**
  - Given an empty extensions list.
  - When `SendNotifications` is called.
  - Then the result is `false`.

### 1.4 SendNotifications handles concurrent extension sends
- **Purpose:** Verify notifications are sent concurrently without blocking.
- **Suggested test name:** `SendNotifications_SendsConcurrentlyToMultipleExtensions`
- **Success checks:**
  - Given multiple extensions with variable response times.
  - When `SendNotifications` is called.
  - Then all extensions receive the notification concurrently.
  - And the slowest extension does not block others.

### 1.5 SendNotifications passes correct notification data to extensions
- **Purpose:** Verify notification title and text are forwarded unchanged.
- **Suggested test name:** `SendNotifications_PassesCorrectNotificationDataToExtensions`
- **Success checks:**
  - Given a notification with specific title and text.
  - When `SendNotifications` is called with that notification.
  - Then each extension receives the exact notification object.
  - And no fields are lost or modified.

---

## 2. API Endpoints

HTTP API contracts for configuring and querying notification extensions.

**See also:** `SHARED_TEST_PATTERNS.md` Section 2 for generic API endpoint testing patterns.

### 2.1 GET /notifications/extensions returns all configured extensions
- **Purpose:** Verify the read endpoint lists all configured extensions.
- **Suggested test name:** `GetExtensions_ReturnsAllConfiguredExtensions`
- **Success checks:**
  - Given multiple extensions are configured in the database.
  - When GET `/api/notifications/extensions` is called.
  - Then the response is 200 OK.
  - And the response body contains all extensions with correct Id, Name, and Type.
  - And the response format is an array.

### 2.2 GET /notifications/extensions returns empty array when none exist
- **Purpose:** Verify read endpoint handles no data gracefully.
- **Suggested test name:** `GetExtensions_ReturnsEmptyArrayWhenNoneExist`
- **Success checks:**
  - Given no extensions are configured.
  - When GET `/api/notifications/extensions` is called.
  - Then the response is 200 OK.
  - And the response body is an empty array `[]`.

### 2.3 PUT /notifications/extensions/naprise creates and validates extension (canonical example)
- **Purpose:** Verify extension creation with full validation (canonical test for all extension types).
- **Suggested test name:** `PutNapriseExtension_CreatesExtensionWithValidation`
- **Success checks:**
  - Given a valid Naprise base URL.
  - When PUT `/api/notifications/extensions/naprise` is called.
  - Then the response is 200 OK or 201 Created.
  - And the extension is persisted with type = Naprise.
  - And subsequent GET returns the new extension.
  - (For invalid URLs, test rejects 400 Bad Request in separate test.)

### 2.4 PUT /notifications/extensions validates URL and rejects invalid input
- **Purpose:** Verify request validation applies to all extension types.
- **Suggested test name:** `PutExtension_Rejects400OnInvalidInput`
- **Success checks:**
  - Given an invalid or empty URL.
  - When any PUT `/api/notifications/extensions/[type]` is called.
  - Then the response is 400 Bad Request.
  - And a validation error message is included.
  - And no extension is created.

### 2.5 PUT /notifications/extensions/discord creates Discord extension
- **Purpose:** Smoke test for Discord extension type persistence.
- **Suggested test name:** `PutDiscordExtension_PersistsCorrectType`
- **Success checks:**
  - Given a valid Discord webhook URL.
  - When PUT `/api/notifications/extensions/discord` is called.
  - Then the extension is created with type = Discord.
  - (Omit duplicate validation checks; rely on 2.4.)

### 2.6 PUT /notifications/extensions/gotify creates Gotify extension
- **Purpose:** Smoke test for Gotify extension type persistence.
- **Suggested test name:** `PutGotifyExtension_PersistsCorrectType`
- **Success checks:**
  - Given a valid Gotify server URL.
  - When PUT `/api/notifications/extensions/gotify` is called.
  - Then the extension is created with type = Gotify.

### 2.7 PUT /notifications/extensions/ntfysh creates NtfySh extension
- **Purpose:** Smoke test for NtfySh extension type persistence.
- **Suggested test name:** `PutNtfyShExtension_PersistsCorrectType`
- **Success checks:**
  - Given a valid NtfySh topic or URL.
  - When PUT `/api/notifications/extensions/ntfysh` is called.
  - Then the extension is created with type = NtfySh.

### 2.8 PUT /notifications/extensions/telegram creates Telegram extension
- **Purpose:** Smoke test for Telegram extension type persistence.
- **Suggested test name:** `PutTelegramExtension_PersistsCorrectType`
- **Success checks:**
  - Given a valid Telegram bot token and chat ID.
  - When PUT `/api/notifications/extensions/telegram` is called.
  - Then the extension is created with type = Telegram.

### 2.9 DELETE /notifications/extensions/{extensionId} removes extension
- **Purpose:** Verify deletion endpoint removes an extension and persistence is correct.
- **Suggested test name:** `DeleteExtension_RemovesExtensionSuccessfully`
- **Success checks:**
  - Given an extension exists with a specific ID.
  - When DELETE `/api/notifications/extensions/{extensionId}` is called.
  - Then the response is 200 OK or 204 No Content.
  - And the extension is removed from the database.
  - And subsequent GET does not return the deleted extension.

### 2.10 DELETE /notifications/extensions/{extensionId} returns 404 for unknown ID
- **Purpose:** Verify deletion error handling.
- **Suggested test name:** `DeleteExtension_Returns404ForUnknownId`
- **Success checks:**
  - Given a UUID that does not correspond to any extension.
  - When DELETE `/api/notifications/extensions/{unknownId}` is called.
  - Then the response is 404 Not Found.
  - And no side effects occur.

### 2.11 All extension endpoints use correct route prefix
- **Purpose:** Verify routes are under `/api/notifications`.
- **Suggested test name:** `Endpoints_AreMappedUnderCorrectServicePrefix`
- **Success checks:**
  - When all service endpoints are inspected.
  - Then all routes begin with `/api/notifications`.
  - And extension routes are under `/api/notifications/extensions`.
  - And no endpoints exist at alternate paths.

### 2.12 Error responses are consistent across all endpoints
- **Purpose:** Verify error handling consistency.
- **Suggested test name:** `Endpoints_ReturnConsistentErrorResponses`
- **Success checks:**
  - Given error conditions (400 Bad Request, 404 Not Found, 500 Internal Error).
  - When endpoints return errors.
  - Then all error responses follow the same structure.
  - And HTTP status codes match expectations.
  - And error messages are descriptive.

---

## 3. Event Handlers

Background workers that react to domain events by sending notifications.

**See also:** `SHARED_TEST_PATTERNS.md` Section 3 for generic event handler patterns.

### 3.1 ChapterDownloadedHandler sends notification on chapter download
- **Purpose:** Verify the handler reacts to chapter download events.
- **Suggested test name:** `ChapterDownloadedHandler_SendsNotificationOnChapterDownloaded`
- **Success checks:**
  - Given a `ChapterDownloadedEvent` is published.
  - When the event handler processes it.
  - Then `SendNotifications` is called on the context.
  - And the handler returns `true` on success.
  - And a properly formatted notification is sent (title includes series/volume/chapter).

### 3.2 ChapterDownloadedHandler formats notification correctly
- **Purpose:** Verify notification message formatting.
- **Suggested test name:** `ChapterDownloadedHandler_FormatsNotificationCorrectly`
- **Success checks:**
  - Given a `ChapterDownloadedEvent` with Series="Dragon Ball", Volume="1", Chapter="10", Title="New Adventure".
  - When the handler formats the notification.
  - Then the title includes "Downloaded Dragon Ball Vol. 1 Ch. 10".
  - And the text is the chapter title ("New Adventure").
  - And no fields are null or malformed.

### 3.3 ChapterDownloadedHandler handles missing Volume gracefully
- **Purpose:** Verify formatting robustness for missing data.
- **Suggested test name:** `ChapterDownloadedHandler_HandlesMissingVolumeGracefully`
- **Success checks:**
  - Given an event with null or missing volume.
  - When the handler formats the notification.
  - Then the notification is still created.
  - And the format gracefully omits the volume (e.g., "Downloaded Dragon Ball Ch. 10").
  - And no null reference exception is thrown.

### 3.4 ChapterDownloadedHandler returns false on send failure
- **Purpose:** Verify error handling.
- **Suggested test name:** `ChapterDownloadedHandler_ReturnsFalseOnSendFailure`
- **Success checks:**
  - Given `SendNotifications` throws or returns false.
  - When the handler processes an event.
  - Then the handler returns `false`.
  - And the error is handled without crashing the event loop.

### 3.5 ChapterDownloadedHandler creates new scope per event
- **Purpose:** Verify scope isolation (prevents resource reuse bugs).
- **Suggested test name:** `ChapterDownloadedHandler_CreatesNewScopePerEvent`
- **Success checks:**
  - Given multiple events are processed in sequence.
  - When each event is processed.
  - Then a new DI scope is created for each.
  - And scoped services are not reused across events.

### 3.6 ChapterDownloadedHandler subscribes to correct RabbitMQ queue
- **Purpose:** Verify event subscription is wired correctly at startup.
- **Suggested test name:** `ChapterDownloadedHandler_SubscribesToCorrectQueue`
- **Success checks:**
  - When the service starts and event handlers are initialized.
  - Then `ChapterDownloadedHandler` is registered.
  - And it subscribes to `ChapterDownloadedEvent` queue.
  - And only one handler processes this queue.

---

## 4. Integration Tests

End-to-end tests that combine multiple components.

### 4.1 Full flow: Chapter downloaded event triggers notifications to all extensions
- **Purpose:** Verify end-to-end event → extensions → notification.
- **Suggested test name:** `Integration_ChapterDownloadEventTriggersNotificationsToAllExtensions`
- **Success checks:**
  - Given multiple notification extensions are configured.
  - When a `ChapterDownloadedEvent` is published.
  - Then the handler receives it.
  - And sends a notification.
  - And all extensions receive the notification.

### 4.2 Extension CRUD operations affect notification dispatch
- **Purpose:** Verify adding/removing extensions changes broadcast behavior.
- **Suggested test name:** `Integration_ExtensionCRUDChangesNotificationDispatch`
- **Success checks:**
  - Given no extensions are configured.
  - When a notification is sent, it broadcasts to zero extensions.
  - When a new extension is added via endpoint.
  - Then subsequent notifications broadcast to exactly one extension.
  - When the extension is deleted via endpoint.
  - Then notifications again broadcast to zero extensions.

---

## Recommended implementation order
1. **Sending Notifications** (1.1–1.5)
   - Highest value: core logic that all other tests depend on.
   - Verify notification dispatch mechanics before testing endpoints and event handlers.
   
2. **API Endpoints** (2.1–2.12)
   - Second priority: users configure extensions via HTTP.
   - See `SHARED_TEST_PATTERNS.md` Section 2 for generic endpoint patterns.
   
3. **Event Handlers** (3.1–3.6)
   - Third priority: verifies background event processing.
   - See `SHARED_TEST_PATTERNS.md` Section 3 for generic event handler patterns.
   
4. **Integration Tests** (4.1–4.2)
   - Final polish: ensures all pieces work together in realistic scenarios.

## Notes

### Sending Notifications specific
- Mock `INotificationExtension` implementations for each type (Naprise, Discord, Gotify, NtfySh, Telegram).
- Use fakes that track call count and capture notification data for assertion.
- Test concurrent dispatch with variable response times to verify non-blocking behavior.

### Endpoint specific
- See `SHARED_TEST_PATTERNS.md` Section 2.3 for extension-type testing patterns.
- Use one canonical example (Naprise) with full validation.
- Use smoke tests for other types (only verify type persistence).
- Use `TestServer` or `WebApplicationFactory<Program>` for integration-style tests.
- Verify persistence by querying the database after endpoint calls.

### Event Handler specific
- Mock `IChannel` to avoid RabbitMQ dependency.
- Use in-memory or test fixture database for `NotificationsContext` queries.
- Test the full handler chain: event deserialization → notification formatting → extension dispatch.

