## Changes

### Bug Fix: Connector enabled states now persist across restarts

**Problem:** The `SetEnabled` API endpoint modified in-memory connector states but never saved them to the database. On restart, all connectors were re-created with `Enabled = true` (the default), making it impossible to permanently disable broken connectors like Mangaworld and WeebCentral.

**Solution:**
- Added new `ConnectorSettings` entity in `API/Schema/ActionsContext/` to store connector enabled/disabled state in the database
- Updated `MangaConnectorController.SetEnabled()` to save state changes to the `ConnectorSettings` table
- Added `LoadConnectorSettings()` method to `Tranga.cs` that runs at startup to restore saved states from the database

### Bug Fix: Search timeout prevents infinite hangs

**Problem:** `Global.cs` search loop used an infinite `do-while` waiting for ALL connector search tasks to complete. If one connector's cover download hung indefinitely (e.g., a slow or unresponsive HTTP request), the entire search would block forever.

**Solution:**
- Added a configurable timeout (default: 10x `HTTP_REQUEST_TIMEOUT`, so ~10 minutes) to the search wait loop
- Search now logs progress every ~5 seconds instead of every 500ms (reduces log spam)
- On timeout, search returns whatever results completed successfully rather than hanging forever

## Files Changed

| File | Change |
|------|--------|
| `API/Schema/ActionsContext/ConnectorSettings.cs` | **New** - DB entity for connector settings |
| `API/Schema/ActionsContext/ActionsContext.cs` | Added `DbSet<ConnectorSettings>` |
| `API/Controllers/MangaConnectorController.cs` | `SetEnabled()` now persists to DB |
| `API/Tranga.cs` | Added `LoadConnectorSettings()` called at startup |
| `API/MangaConnectors/Global.cs` | Search loop with timeout + periodic logging |
