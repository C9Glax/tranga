# Services.Libraries.Tests Implementation Backlog

This backlog is ordered by priority and intended to be implemented top-to-bottom.

**See also:** `SHARED_TEST_PATTERNS.md` for reusable test patterns used across all services.

---

## 1. Entities

Library configuration and domain models.

See `SHARED_TEST_PATTERNS.md` Section 1 for generic record immutability pattern.

### 1.1 Library record construction and immutability
- **Purpose:** Verify Library record is a proper value type with required fields.
- **Suggested test name:** `Library_CanBeConstructedAndIsImmutable`
- **Success checks:**
  - Can construct Library with LibraryServiceType, Id, and BaseUrl.
  - All properties are accessible and correct.
  - Library behaves as C# record (value equality, immutability).
  - Two instances with same values are equal.

---

## 2. Event Handlers

Background workers that react to download events and sync with library services.

**See also:** `SHARED_TEST_PATTERNS.md` Section 3 for generic event handler patterns.

### 2.1 ChapterDownloadedHandler processes ChapterDownloadedEvent and syncs library
- **Purpose:** Verify handler reacts to chapter downloads and triggers library scans.
- **Suggested test name:** `ChapterDownloadedHandler_ProcessesEventAndSyncsLibrary`
- **Success checks:**
  - Given a `ChapterDownloadedEvent` is published with a valid MangaId.
  - When the event handler processes it.
  - Then the handler returns `true` on success.
  - And all configured library services are checked.
  - And Komga library is scanned/updated.

### 2.2 ChapterDownloadedHandler discovers new manga and creates mapping
- **Purpose:** Verify handler detects new manga in Komga library.
- **Suggested test name:** `ChapterDownloadedHandler_DiscoversNewMangaAndCreatesMappings`
- **Success checks:**
  - Given manga has no existing mapping to a Komga library.
  - When the handler processes an event.
  - Then library is scanned.
  - And new series is detected (by comparing pre/post scan results).
  - And `MangaIdMapping` is created linking Tranga MangaId to Komga SeriesId.

### 2.3 ChapterDownloadedHandler rescans without creating duplicate mapping
- **Purpose:** Verify subsequent downloads reuse existing mappings.
- **Suggested test name:** `ChapterDownloadedHandler_RescansWithoutDuplicateMappings`
- **Success checks:**
  - Given manga already has a mapping to Komga library.
  - When the handler processes an event.
  - Then library is scanned.
  - And no new mapping is created.
  - And existing mapping is reused.

### 2.4 ChapterDownloadedHandler handles unsupported library types gracefully
- **Purpose:** Verify handler skips unknown library types.
- **Suggested test name:** `ChapterDownloadedHandler_SkipsUnsupportedLibraryTypes`
- **Success checks:**
  - Given a library service of unsupported type.
  - When an event is processed.
  - Then the handler skips it and continues to next library.
  - And returns `true` (processing succeeded).
  - And no error is thrown.

### 2.5 ChapterDownloadedHandler creates new scope per event
- **Purpose:** Verify scope isolation (prevents resource reuse bugs).
- **Suggested test name:** `ChapterDownloadedHandler_CreatesNewScopePerEvent`
- **Success checks:**
  - Given multiple events processed sequentially.
  - When each event is processed.
  - Then a new DI scope is created for each.
  - And scoped services (like LibrariesContext) are not reused across events.

### 2.6 ChapterDownloadedHandler returns false on error
- **Purpose:** Verify error handling.
- **Suggested test name:** `ChapterDownloadedHandler_ReturnsFalseOnError`
- **Success checks:**
  - Given database error, Komga connection failure, or other exception.
  - When the handler processes an event.
  - Then the handler returns `false`.
  - And the error is handled without crashing the event loop.

### 2.7 ChapterDownloadedHandler subscribes to correct RabbitMQ queue
- **Purpose:** Verify event subscription is wired correctly at startup.
- **Suggested test name:** `ChapterDownloadedHandler_SubscribesToCorrectQueue`
- **Success checks:**
  - When the service starts and event handlers are initialized.
  - Then `ChapterDownloadedHandler` is instantiated.
  - And it subscribes to `ChapterDownloadedEvent` queue.
  - And only one handler processes this queue.

---

## 3. Helpers

Conversion utilities for domain-to-external mappings.

### 3.1 DbLibraryToLibraryExtension converts to Komga
- **Purpose:** Verify database library converts to extension instance.
- **Suggested test name:** `DbLibraryToLibraryExtension_ConvertsToKomga`
- **Success checks:**
  - Given a DbLibraryService with Komga type.
  - When `ToExtension()` is called.
  - Then a Komga extension instance is returned.
  - And BaseUrl is correctly passed.

### 3.2 DbLibraryToLibraryExtension returns null for non-Komga
- **Purpose:** Verify null is returned for unsupported types.
- **Suggested test name:** `DbLibraryToLibraryExtension_ReturnsNullForNonKomga`
- **Success checks:**
  - Given a DbLibraryService of unsupported type.
  - When `ToExtension()` is called.
  - Then `null` is returned.

## 3. Helpers

Conversion utilities for domain-to-external mappings.

See `SHARED_TEST_PATTERNS.md` Section 4 for generic helper conversion pattern.

### 3.1 DbLibraryToLibraryExtension converts to Komga correctly
- **Purpose:** Verify database library converts to extension instance with correct configuration.
- **Suggested test name:** `DbLibraryToLibraryExtension_ConvertsToKomgaCorrectly`
- **Success checks:**
  - Given a DbLibraryService with type = Komga.
  - When `ToExtension()` is called.
  - Then a Komga extension instance is returned.
  - And BaseUrl is correctly passed to the extension.
  - And extension is ready to use (not null or malformed).

### 3.2 DbLibraryToLibraryExtension returns null for unsupported types
- **Purpose:** Verify robustness for unsupported library types.
- **Suggested test name:** `DbLibraryToLibraryExtension_ReturnsNullForUnsupportedTypes`
- **Success checks:**
  - Given a DbLibraryService with unsupported type.
  - When `ToExtension()` is called.
  - Then `null` is returned (not exception).
  - And handler can safely check for null.

---

## 4. API Endpoints

HTTP API contracts for library configuration and management.

See `SHARED_TEST_PATTERNS.md` Section 2 for generic API endpoint testing patterns.

### 4.1 GET /libraries returns all configured libraries
- **Purpose:** Verify read endpoint lists all libraries.
- **Suggested test name:** `GetLibraries_ReturnsAllConfiguredLibraries`
- **Success checks:**
  - Given multiple libraries are configured in the database.
  - When GET `/api/libraries` is called.
  - Then the response is 200 OK.
  - And all libraries are returned with Id, BaseUrl, and LibraryServiceType.

### 4.2 GET /libraries returns empty array when none configured
- **Purpose:** Verify empty-state handling.
- **Suggested test name:** `GetLibraries_ReturnsEmptyWhenNoneConfigured`
- **Success checks:**
  - Given no libraries are configured.
  - When GET `/api/libraries` is called.
  - Then the response is 200 OK.
  - And an empty array is returned.

### 4.3 PUT /libraries/komga creates Komga library successfully
- **Purpose:** Verify Komga library creation endpoint.
- **Suggested test name:** `PutKomga_CreatesLibrarySuccessfully`
- **Success checks:**
  - Given a valid Komga base URL (e.g., "http://localhost:8080").
  - When PUT `/api/libraries/komga` is called.
  - Then the response is 200 OK or 201 Created.
  - And the library is persisted with type = Komga.
  - And subsequent GET returns the new library.

### 4.4 PUT /libraries/komga validates URL and rejects invalid input
- **Purpose:** Verify request validation on creation.
- **Suggested test name:** `PutKomga_Rejects400OnInvalidInput`
- **Success checks:**
  - Given an invalid or empty URL.
  - When PUT `/api/libraries/komga` is called.
  - Then the response is 400 Bad Request.
  - And validation error is included.
  - And no library is created.

### 4.5 PUT /libraries/komga rejects duplicate configurations (if applicable)
- **Purpose:** Verify duplicate prevention (implementation-dependent).
- **Suggested test name:** `PutKomga_PreventsDuplicateConfigurations`
- **Success checks:**
  - Given a library with specific URL already exists.
  - When PUT with same URL is called.
  - Then appropriate error is returned (400, 409, or implementation-specific).
  - And no duplicate is created.
  - (Note: If duplicates are allowed, remove this test.)

### 4.6 DELETE /libraries/{libraryId} removes library successfully
- **Purpose:** Verify deletion endpoint.
- **Suggested test name:** `DeleteLibrary_RemovesLibrarySuccessfully`
- **Success checks:**
  - Given a library exists with known ID.
  - When DELETE `/api/libraries/{libraryId}` is called.
  - Then the response is 200 OK or 204 No Content.
  - And the library is removed from the database.
  - And subsequent GET does not return it.

### 4.7 DELETE /libraries/{libraryId} returns 404 for unknown ID
- **Purpose:** Verify deletion error handling.
- **Suggested test name:** `DeleteLibrary_Returns404ForUnknownId`
- **Success checks:**
  - Given a UUID that does not correspond to any library.
  - When DELETE is called with that ID.
  - Then the response is 404 Not Found.

### 4.8 All endpoints use correct route prefix
- **Purpose:** Verify routes are under `/api/libraries`.
- **Suggested test name:** `Endpoints_AreMappedUnderCorrectServicePrefix`
- **Success checks:**
  - When all service endpoints are inspected.
  - Then all routes begin with `/api/libraries`.
  - And no endpoints exist at alternate paths.

### 4.9 Error responses are consistent across endpoints
- **Purpose:** Verify error handling consistency.
- **Suggested test name:** `Endpoints_ReturnConsistentErrorResponses`
- **Success checks:**
  - Given various error conditions (400, 404, 500).
  - When endpoints return errors.
  - Then all error responses follow the same format.
  - And HTTP status codes match expectations.

---

## 5. Integration Tests

End-to-end tests combining multiple components.

### 5.1 Full flow: Library configuration and event sync
- **Purpose:** Verify end-to-end library configuration and event processing.
- **Suggested test name:** `Integration_LibraryConfigurationAndEventSync`
- **Success checks:**
  - Given a Komga library is added via endpoint.
  - When a ChapterDownloadedEvent is published.
  - Then the handler uses the configured library.
  - And Komga is notified and scanned.

### 5.2 CRUD operations affect event handler behavior
- **Purpose:** Verify adding/removing libraries changes handler behavior.
- **Suggested test name:** `Integration_LibraryCRUDChangesEventHandling`
- **Success checks:**
  - Given no libraries are configured.
  - When an event is processed, no libraries are scanned.
  - When a library is added via endpoint.
  - Then subsequent events trigger that library scan.
  - When the library is deleted.
  - Then it is no longer scanned.

---

## Recommended implementation order
1. **Entities** (1.1)
   - Foundation: Library record used throughout.
   
2. **Helpers** (3.1–3.2)
   - Conversion logic needed for event handlers and endpoints.
   
3. **API Endpoints** (4.1–4.9)
   - User-facing HTTP contracts.
   - See `SHARED_TEST_PATTERNS.md` Section 2 for generic patterns.
   
4. **Event Handlers** (2.1–2.7)
   - Background event processing.
   - See `SHARED_TEST_PATTERNS.md` Section 3 for generic patterns.
   
5. **Integration Tests** (5.1–5.2)
   - End-to-end workflows.

## Notes

### Event Handler specific
- Mock Komga extension to avoid HTTP calls to real Komga instance.
- Mock LibrariesContext with test data for database queries.
- Mock time-based operations (Thread.Sleep for retries) for deterministic tests.
- Verify database state changes after event processing (MangaIdMapping created/updated).

### Endpoint specific
- Use `TestServer` or `WebApplicationFactory<Program>` for integration-style tests.
- Verify persistence by querying database after endpoint calls.
- See `SHARED_TEST_PATTERNS.md` Section 2 for generic API testing patterns.

