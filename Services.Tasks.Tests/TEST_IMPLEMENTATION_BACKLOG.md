# Services.Tasks.Tests Implementation Backlog

This backlog is ordered by priority and intended to be implemented top-to-bottom.

**See also:** `SHARED_TEST_PATTERNS.md` for reusable test patterns used across all services.

---

## 1. `TaskQueue`

### 1.1 Enqueues a task
- **Purpose:** Verify a task can be added to the queue.
- **Suggested test name:** `Enqueue_AddsTaskToQueue`
- **Checks:**
  - Queue accepts a valid task instance.
  - Queue state reflects one pending item.

### 1.2 Dequeues the task with the highest priority
- **Purpose:** Verify priority ordering is respected.
- **Suggested test name:** `Dequeue_ReturnsHighestPriorityTaskFirst`
- **Checks:**
  - Multiple tasks with different priorities can be enqueued.
  - The first dequeued task is the highest priority item.
  - If priorities tie, the tie-break behavior is asserted explicitly.

### 1.3 Returns empty / null when queue is empty
- **Purpose:** Verify empty-queue behavior is safe and predictable.
- **Suggested test name:** `Dequeue_ReturnsNullWhenQueueIsEmpty`
- **Checks:**
  - Dequeue on an empty queue returns the documented empty value.
  - No exception is thrown for an empty queue state.

### 1.4 Handles duplicate tasks correctly
- **Purpose:** Verify duplicate handling matches the contract.
- **Suggested test name:** `Enqueue_HandlesDuplicateTasksAccordingToContract`
- **Checks:**
  - Duplicate task submission is either accepted, ignored, or deduplicated as intended.
  - Behavior is consistent across repeated operations.

### 1.5 Thread-safety / concurrent enqueue behavior, if applicable
- **Purpose:** Verify concurrent queue access does not lose tasks.
- **Suggested test name:** `Enqueue_IsSafeUnderConcurrentAccess`
- **Checks:**
  - Concurrent enqueue operations complete without corruption.
  - Final queue contents match the number of successful enqueues.
  - Any documented ordering guarantees are preserved.

---

## 2. `TasksCollection`

### 2.1 Adds a task type to the collection
- **Purpose:** Verify registration works.
- **Suggested test name:** `Add_RegistersTaskType`
- **Checks:**
  - A task type can be inserted into the collection.
  - The collection reflects the new registration.

### 2.2 Retrieves a task by key
- **Purpose:** Verify lookup by key works.
- **Suggested test name:** `Get_ReturnsRegisteredTaskByKey`
- **Checks:**
  - Lookup using a valid key returns the expected task.
  - Key matching rules are honored.

### 2.3 Returns null / not found for unknown tasks
- **Purpose:** Verify missing-key behavior.
- **Suggested test name:** `Get_ReturnsNullForUnknownTask`
- **Checks:**
  - Unknown keys return the documented empty value.
  - No exception is thrown for missing entries.

### 2.4 Enumerates all registered tasks
- **Purpose:** Verify collection enumeration is complete.
- **Suggested test name:** `Enumerate_ReturnsAllRegisteredTasks`
- **Checks:**
  - All registered task types appear in the enumeration.
  - No unregistered tasks are included.
  - Ordering is asserted if the API guarantees one.

---

## 3. `Tasks`

### 3.1 `ExecuteAsync` scope refreshes
- **Purpose:** Verify each execution uses a refreshed scope when required.
- **Suggested test name:** `ExecuteAsync_CreatesFreshScopePerRun`
- **Checks:**
  - A new DI scope is created for execution.
  - Scoped dependencies are resolved from the fresh scope.
  - Disposed scope instances are not reused.

### 3.2 `LastRun` is updated correctly
- **Purpose:** Verify execution bookkeeping is correct.
- **Suggested test name:** `ExecuteAsync_UpdatesLastRunOnSuccess`
- **Checks:**
  - `LastRun` is set or refreshed after execution.
  - Update timing matches the documented behavior.
  - Failure behavior is covered separately if `LastRun` should not update on errors.

---

## 4. `Endpoints`

### 4.1 GET endpoints return expected status codes and payloads
- **Purpose:** Verify read endpoints follow the API contract.
- **Suggested test name:** `GetEndpoints_ReturnExpectedStatusCodesAndPayloads`
- **Checks:**
  - Known resources return success codes.
  - Response payloads contain the expected shape and values.
  - Empty or missing results use the correct status code.

### 4.2 POST endpoints validate input and reject bad requests
- **Purpose:** Verify create/update validation is enforced.
- **Suggested test name:** `PostEndpoints_RejectInvalidRequests`
- **Checks:**
  - Invalid payloads return a client error response.
  - Validation messages are consistent with the service contract.
  - Valid payloads still succeed.

### 4.3 DELETE / cancel endpoints behave correctly
- **Purpose:** Verify mutation and cancellation routes work as intended.
- **Suggested test name:** `DeleteOrCancelEndpoints_ApplyExpectedSideEffects`
- **Checks:**
  - Valid targets are deleted or cancelled successfully.
  - Unknown targets return the expected failure response.
  - Side effects are observable in the service layer.

### 4.4 Routes are mounted under the expected prefix
- **Purpose:** Verify endpoint routing matches the gateway/service contract.
- **Suggested test name:** `Endpoints_AreMappedUnderExpectedPrefix`
- **Checks:**
  - Route prefixes match the service API convention.
  - Endpoint paths do not drift from the gateway configuration.

### 4.5 Error responses are consistent for not-found / invalid-request cases
- **Purpose:** Verify error handling is predictable.
- **Suggested test name:** `Endpoints_ReturnConsistentErrorResponses`
- **Checks:**
  - Not-found responses use the expected status code and payload.
  - Invalid request responses use the expected status code and payload.
  - Error format is consistent across endpoints.

---

## 5. Event Handlers

Background workers that react to domain events by creating and queuing tasks.

**See also:** `SHARED_TEST_PATTERNS.md` Section 3 for generic event handler patterns.

### 5.1 DownloadLinkModifiedHandler processes DownloadLinkModifiedEvent
- **Purpose:** Verify handler reacts to download link modification events and creates tasks.
- **Suggested test name:** `DownloadLinkModifiedHandler_ProcessesEventSuccessfully`
- **Success checks:**
  - Given a `DownloadLinkModifiedEvent` is published with a valid DownloadLinkId.
  - When the event handler processes it.
  - Then the handler returns `true` on success.
  - And a new `GetMangaChaptersTask` is created for the affected manga.
  - And the task is added to `TasksCollection.RunOnceTasks` (not the main queue).

### 5.2 DownloadLinkModifiedHandler extracts correct manga and creates correct task
- **Purpose:** Verify data extraction and task creation details.
- **Suggested test name:** `DownloadLinkModifiedHandler_ExtractsDataAndCreatesTask`
- **Success checks:**
  - Given a download link with a known MangaId.
  - When the handler processes the event.
  - Then the task's MangaId matches the download link's MangaId.
  - And the task is uniquely identifiable (TaskId is set).
  - And the task targets the correct manga (not the wrong one).

### 5.3 DownloadLinkModifiedHandler returns false when download link not found
- **Purpose:** Verify graceful error handling for missing data.
- **Suggested test name:** `DownloadLinkModifiedHandler_ReturnsFalseOnMissingDownloadLink`
- **Success checks:**
  - Given a `DownloadLinkModifiedEvent` with a DownloadLinkId that does not exist.
  - When the handler processes it.
  - Then the handler returns `false`.
  - And no task is created.
  - And no exception is thrown (error is handled gracefully).

### 5.4 DownloadLinkModifiedHandler creates fresh scope per event
- **Purpose:** Verify scope isolation (prevents resource reuse bugs).
- **Suggested test name:** `DownloadLinkModifiedHandler_CreatesNewScopePerEvent`
- **Success checks:**
  - Given multiple events processed sequentially.
  - When each is processed.
  - Then a new DI scope is created for each.
  - And scoped services (like MangaContext) are not reused across events.
  - And disposed scope instances are not accessed in subsequent events.

### 5.5 DownloadLinkModifiedHandler subscribes to correct RabbitMQ queue
- **Purpose:** Verify event subscription is wired correctly at startup.
- **Suggested test name:** `DownloadLinkModifiedHandler_SubscribesToCorrectQueue`
- **Success checks:**
  - When the service starts and event handlers are registered.
  - Then `DownloadLinkModifiedHandler` is instantiated.
  - And it subscribes to the `DownloadLinkModifiedEvent` queue.
  - And it does not subscribe to unrelated queues (e.g., ChapterDownloadedEvent).
  - And only one handler processes this event (no duplicate handlers).

---

## Recommended implementation order
1. `TaskQueue` (1.1–1.5)
2. `TasksCollection` (2.1–2.4)
3. `Tasks` (3.1–3.2)
4. `Endpoints` (4.1–4.5)
5. `Event Handlers` (5.1–5.5)

## Notes

### TaskQueue specific
- Prefer focused unit tests: no need to test against real task implementations.
- Mock or stub ITask for queue tests.
- Test priority ordering exhaustively (verify tie-break behavior, edge cases).

### Event Handler specific
- Mock `IChannel` to avoid RabbitMQ dependency.
- Use in-memory or test fixture database for `MangaContext` queries.
- Mock or fake the scope creation to verify scope isolation without complex DI setup.
- Test the full handler chain: event deserialization → database lookup → task creation → queue addition.

### Endpoint specific
- Use `TestServer` or `WebApplicationFactory<Program>` for integration-style tests.
- Verify persistence by querying the database after endpoint calls.
- See `SHARED_TEST_PATTERNS.md` Section 2 for generic API endpoint testing patterns.

