# SHARED_TEST_PATTERNS.md

This document defines reusable test patterns and conventions used across all Tranga test backlogs.

---

## 1. Record/Entity Immutability Pattern

Applied to: SearchQuery, Language, Library, Manga, Chapter, Metadata, DownloadLink, Notification, etc.

### Pattern Template
```
### X.1 [Record] construction with required fields
- **Purpose:** Verify [Record] can be created with all required fields.
- **Suggested test name:** `[Record]_CanBeConstructedWithRequiredFields`
- **Success checks:**
  - Can construct with [all required parameters].
  - All properties are accessible after construction.
  - Null parameters are handled according to contract (required vs. optional).
  - Field constraints (length limits, formats) are enforced.

### X.2 [Record] immutability
- **Purpose:** Verify [Record] cannot be modified after construction.
- **Suggested test name:** `[Record]_IsImmutable`
- **Success checks:**
  - [Record] is a C# record (value type semantics).
  - Once created, properties cannot be modified.
  - Two instances with identical values are equal (value equality).
  - Equality comparison works correctly.
```

### Usage Notes
- All domain models in Tranga use C# records for value type semantics.
- No need to test immutability separately if using `record` keyword and `init` accessor.
- Focus on boundary conditions: min/max lengths, null handling, format validation.

---

## 2. API Endpoint Testing Pattern

Applied to: All services (Manga, Notifications, Libraries, Tasks, etc.)

### 2.1 Standard CRUD Endpoints Pattern

#### List/Get-All Endpoint
```
### X.1 GET /resource returns all items
- **Purpose:** Verify read endpoint lists all resources.
- **Suggested test name:** `GetResourceList_ReturnsAll[Resources]`
- **Success checks:**
  - When GET `/api/resource` is called.
  - Then the response is 200 OK.
  - And all resources are returned in expected format (array/collection).
  - And each item includes required fields.

### X.2 GET /resource returns empty array when none exist
- **Purpose:** Verify empty state handling.
- **Suggested test name:** `GetResourceList_ReturnsEmptyWhenNoneExist`
- **Success checks:**
  - When no resources exist in the database.
  - Then the response is 200 OK.
  - And an empty array is returned.
  - No error status is returned for empty state.
```

#### Get-By-ID Endpoint
```
### X.3 GET /resource/{id} returns specific resource
- **Purpose:** Verify single resource retrieval by ID.
- **Suggested test name:** `GetResource_ReturnsSpecificResourceById`
- **Success checks:**
  - Given a resource with known ID.
  - When GET `/api/resource/{id}` is called.
  - Then the response is 200 OK.
  - And the specific resource is returned.
  - And all fields are populated.

### X.4 GET /resource/{id} returns 404 for unknown ID
- **Purpose:** Verify not-found error handling.
- **Suggested test name:** `GetResource_Returns404ForUnknownId`
- **Success checks:**
  - When a non-existent ID is queried.
  - Then the response is 404 Not Found.
  - And response body includes error information.
```

#### Create/PUT Endpoint
```
### X.5 PUT /resource creates resource successfully
- **Purpose:** Verify resource creation endpoint.
- **Suggested test name:** `PutResource_CreatesResourceSuccessfully`
- **Success checks:**
  - Given a valid request payload.
  - When PUT `/api/resource` is called.
  - Then the response is 200 OK or 201 Created.
  - And the resource is persisted to database.
  - And subsequent GET returns the created resource.

### X.6 PUT /resource validates input and rejects bad requests
- **Purpose:** Verify request validation on creation.
- **Suggested test name:** `PutResource_Rejects400OnInvalidInput`
- **Success checks:**
  - Given an invalid request payload (missing required fields, wrong format).
  - When PUT `/api/resource` is called.
  - Then the response is 400 Bad Request.
  - And validation error details are included.
  - And no resource is created.
```

#### Delete Endpoint
```
### X.7 DELETE /resource/{id} removes resource
- **Purpose:** Verify resource deletion.
- **Suggested test name:** `DeleteResource_RemovesResourceSuccessfully`
- **Success checks:**
  - Given a resource exists.
  - When DELETE `/api/resource/{id}` is called.
  - Then the response is 200 OK or 204 No Content.
  - And the resource is removed from database.
  - And subsequent GET returns 404.

### X.8 DELETE /resource/{id} returns 404 for unknown ID
- **Purpose:** Verify deletion error handling.
- **Suggested test name:** `DeleteResource_Returns404ForUnknownId`
- **Success checks:**
  - When a non-existent ID is deleted.
  - Then the response is 404 Not Found.
  - And no side effects occur.
```

### 2.2 Common Endpoint Tests

#### Route Prefix Verification (single test per service)
```
### X.N All endpoints use correct route prefix
- **Purpose:** Verify routes are mounted under expected service prefix.
- **Suggested test name:** `Endpoints_AreMappedUnderCorrectServicePrefix`
- **Success checks:**
  - When all service endpoints are inspected.
  - Then all routes begin with `/api/[service]` (e.g., `/api/mangas`, `/api/notifications`).
  - And routes do not drift from gateway configuration.
  - And no endpoints exist at alternate paths.
```

#### Error Response Consistency (single test per service)
```
### X.N Error responses follow consistent format
- **Purpose:** Verify error responses are predictable.
- **Suggested test name:** `Endpoints_ReturnConsistentErrorResponses`
- **Success checks:**
  - Given various error conditions (400, 404, 500).
  - When endpoints return errors.
  - Then all error responses follow the same structure.
  - And HTTP status codes match expected values.
  - And error messages are descriptive.
  - And no confidential information leaks in error responses.
```

### 2.3 Extension-Specific Tests

When a service supports multiple extension types (e.g., Naprise, Discord, Gotify in Notifications):
- Test one canonical example (e.g., Naprise) with full validation.
- For others, test creation only (type is correctly stored).
- Do not repeat validation tests for each extension type.

Example:
```
### 2.3 PUT /notifications/extensions/naprise creates extension
- **Purpose:** Canonical example of extension creation with full validation.
- **Suggested test name:** `PutNapriseExtension_CreatesAndValidatesSuccessfully`
- [Full validation checks]

### 2.4 PUT /notifications/extensions/discord creates Discord extension
- **Purpose:** Quick smoke test that Discord type persists correctly.
- **Suggested test name:** `PutDiscordExtension_PersistsCorrectType`
- **Success checks:**
  - Given valid Discord webhook URL.
  - When PUT is called.
  - Then extension is created with type=Discord.
  - (Omit duplicate validation checks from 2.3)
```

---

## 3. Event Handler Testing Pattern

Applied to: DownloadLinkModifiedHandler (Tasks), ChapterDownloadedHandler (Notifications, Libraries)

### 3.1 Standard Event Handler Structure

Every event handler test suite should include:

#### Core Event Processing (handler-specific)
```
### X.1 [Handler] processes [Event] successfully
- **Purpose:** Verify handler reacts to domain event.
- **Suggested test name:** `[Handler]_Processes[Event]Successfully`
- **Success checks:**
  - Given a `[Event]` is published.
  - When the handler processes it.
  - Then the handler returns `true` on success.
  - And side effects are applied (e.g., task created, notification sent, library scanned).

### X.2 [Handler] creates correct artifact for [Event]
- **Purpose:** Verify the correct action/artifact is produced.
- **Suggested test name:** `[Handler]_Creates[Artifact]For[Event]`
- **Success checks:**
  - Given a `[Event]` with specific data.
  - When the handler processes it.
  - Then [specific artifact] is created with correct properties.
  - And the artifact is stored/queued correctly.
```

#### Error Handling (shared pattern - implement once per handler)
```
### X.N [Handler] creates new scope per event
- **Purpose:** Verify scope isolation for each event.
- **Suggested test name:** `[Handler]_CreatesNewScopePerEvent`
- **Success checks:**
  - Given multiple events processed sequentially.
  - When each event is processed.
  - Then a new DI scope is created for each.
  - And scoped services are not reused across events.
  - And disposed scopes are not accessed.

### X.N [Handler] returns false on exception
- **Purpose:** Verify error handling.
- **Suggested test name:** `[Handler]_ReturnsFalseOnException`
- **Success checks:**
  - Given an exception occurs during processing.
  - When the handler catches it.
  - Then `false` is returned (not thrown).
  - And the error does not crash the event loop.

### X.N [Handler] subscribes to correct RabbitMQ queue
- **Purpose:** Verify event subscription is configured.
- **Suggested test name:** `[Handler]_SubscribesToCorrectQueue`
- **Success checks:**
  - When the service starts.
  - Then `[Handler]` is registered.
  - And it subscribes to `[Event]` queue.
  - And no other handlers listen to this queue (single responsibility).
```

### 3.2 Shared Event Handler Fixtures

For all event handler tests, use:
- **Mock IChannel:** Avoid RabbitMQ dependency.
- **Mock database context:** Use in-memory provider with test data.
- **Mock scoped service provider:** Verify scope creation without complex DI setup.
- **Fake clock (if time-sensitive):** Deterministic timing for retry/delay tests.

---

## 4. Helper/Conversion Testing Pattern

Applied to: Helpers in all services (ChapterInfoHelper, MangaDTOHelper, DbLibraryToLibraryExtension, etc.)

### Pattern Template
```
### X.1 [Helper] converts [SourceType] to [TargetType] correctly
- **Purpose:** Verify conversion preserves data.
- **Suggested test name:** `[Helper]_Converts[SourceType]To[TargetType]`
- **Success checks:**
  - Given a valid [SourceType] instance.
  - When `[HelperMethod]()` is called.
  - Then a [TargetType] is returned.
  - And all mappable fields are correctly transferred.
  - And new IDs are generated where required (e.g., UUIDs).
  - And defaults are applied for unmapped fields.

### X.2 [Helper] handles null/invalid inputs gracefully
- **Purpose:** Verify robustness.
- **Suggested test name:** `[Helper]_HandlesNullOrInvalidInputs`
- **Success checks:**
  - Given null or invalid [SourceType].
  - When `[HelperMethod]()` is called.
  - Then either null is returned or appropriate error is thrown.
  - And no partial/corrupted objects are created.
```

### Usage Notes
- Converters should be simple: focus on data mapping, not business logic.
- If conversion is complex (multiple steps), break into smaller helpers.
- Document any data loss or transformation (e.g., "CoverId may be null after conversion if not present in source").

---

## 5. Integration Test Pattern

Applied to: All services (at least 2 per service showing end-to-end flow)

### Pattern Template
```
### 5.1 Full flow: [Scenario 1]
- **Purpose:** Verify end-to-end workflow from user action to side effect.
- **Suggested test name:** `Integration_[Scenario1]_EndToEnd`
- **Success checks:**
  - Given [setup: data, configuration, mocks].
  - When [user action: API call, event published].
  - Then [expected outcome 1] occurs.
  - And [expected outcome 2] occurs.
  - And [expected outcome 3] occurs.
  - And all intermediate layers (database, event bus, extensions) behave correctly.

### 5.2 CRUD operations affect downstream behavior
- **Purpose:** Verify configuration changes cascade correctly.
- **Suggested test name:** `Integration_CRUDOperationsAffectDownstream`
- **Success checks:**
  - Given initial state (e.g., no resources).
  - When a create operation occurs.
  - Then subsequent operations reflect the new state.
  - When the resource is modified.
  - Then dependent behavior updates.
  - When the resource is deleted.
  - Then dependent behavior reverts to initial state.
```

### Usage Notes
- Integration tests often use `WebApplicationFactory<T>` or `TestServer` for HTTP testing.
- Always clean up test data (database) between test runs.
- Test realistic user workflows, not just isolated units.
- Verify both happy path and error paths where practical.

---

## 6. Naming Conventions

### Test Method Names
Use the pattern: `[ComponentUnderTest]_[Scenario]_[ExpectedOutcome]`

Examples:
- ✅ `Manga_CanBeConstructedWithRequiredFields`
- ✅ `GetMangaList_ReturnsAllManga`
- ✅ `ChapterDownloadedHandler_ProcessesEventSuccessfully`
- ✅ `Integration_ChapterDownloadEventTriggersNotifications`

Avoid:
- ❌ `Test1`, `TestManga`, `TestAll` (too vague)
- ❌ `MangaShouldConstructCorrectly` (redundant "should")
- ❌ `WhenICallGetMangaThenItReturnsCorrectly` (too verbose)

### Test File Organization
- One test class per component (e.g., `MangaDTOHelperTests`, `ChapterDownloadedHandlerTests`).
- Use `[Theory]` for parameterized tests to reduce duplication.
- Use nested classes or regions to organize test groups by feature.

---

## 7. Cross-Service Test Reuse

### When to Generalize
- **Record immutability**: Common pattern, but each record is tested in its own service's test suite (do not centralize).
- **API endpoint CRUD**: Common pattern, but each service implements its own endpoints. Reuse the template, not the tests.
- **Event handler scope isolation**: Implement once per handler in each service's suite.

### When NOT to Generalize
- Do not create shared test fixtures that couple services together.
- Do not assume tests in Common.Tests run before service tests.
- Each service's test suite must be independently runnable.

---

## 8. Test Execution and Maintenance

### Before implementing a test
1. Check if the pattern already exists in this document.
2. Check if a similar test exists in another service's backlog.
3. Reuse the pattern/template; adapt as needed for the specific component.

### When adding a new test pattern
1. Add it to this document (SHARED_TEST_PATTERNS.md).
2. Update all affected service backlogs to reference it.
3. Use consistent naming and structure across all services.

### When a test pattern is updated
1. Update this document.
2. Update all service backlogs that reference it.
3. Notify the team of the change so new tests use the updated pattern.

