# Services.Manga.Tests Implementation Backlog

This backlog is ordered by priority and intended to be implemented top-to-bottom.

**See also:** `SHARED_TEST_PATTERNS.md` for reusable test patterns used across all services.

---

## 1. Entities

Domain models for manga, chapters, metadata, and downloads.

See `SHARED_TEST_PATTERNS.md` Section 1 for generic record immutability pattern.

### 1.1 Manga record construction
- **Purpose:** Verify Manga record with required fields.
- **Suggested test name:** `Manga_CanBeConstructedWithRequiredFields`
- **Success checks:**
  - Can construct with MangaId and Monitored.
  - Can optionally include MetadataEntry and DownloadLinks.
  - All properties are accessible.
  - Monitored boolean is correctly stored.

### 1.2 Chapter record construction and field validation
- **Purpose:** Verify Chapter record with field constraints.
- **Suggested test name:** `Chapter_CanBeConstructedWithFieldConstraints`
- **Success checks:**
  - Can construct with ChapterId, MangaId, Volume, Number.
  - Title accepts up to 2048 characters.
  - Volume accepts up to 16 characters (various formats: "1", "1.5", "Special").
  - Number is required and accepts various formats (up to 16 chars).
  - ReleaseDate is nullable DateTimeOffset.
  - Exceeding limits fails validation.

### 1.3 Metadata record construction and field validation
- **Purpose:** Verify Metadata record with field constraints and optional fields.
- **Suggested test name:** `Metadata_CanBeConstructedWithFieldConstraints`
- **Success checks:**
  - Can construct with all required fields (MetadataId, ExtensionId, Identifier, Series).
  - Series accepts up to 1024 characters; exceeding fails validation.
  - Summary is optional and accepts up to 4096 characters.
  - Year, ChaptersNumber, Language are nullable.
  - Genres, Authors, Artists are arrays (can be empty).
  - Status, NSFW are nullable booleans.
  - CoverId is nullable Guid.

### 1.4 DownloadLink record construction and field validation
- **Purpose:** Verify DownloadLink record with field constraints.
- **Suggested test name:** `DownloadLink_CanBeConstructedWithFieldConstraints`
- **Success checks:**
  - Can construct with all required fields (DownloadId, ExtensionId, Identifier, Series, Url).
  - Series accepts up to 1024 characters; exceeding fails validation.
  - Summary is optional and accepts up to 4096 characters.
  - Language is optional and accepts up to 8 characters.
  - Url is nullable string.
  - CoverId is nullable Guid.
  - NSFW is nullable boolean.

---

## 2. Helpers

Conversion utilities for DTOs and database entities.

See `SHARED_TEST_PATTERNS.md` Section 4 for generic helper conversion pattern.

### 2.1 ChapterInfoHelper converts ChapterInfo to DbChapter and links correctly
- **Purpose:** Verify conversion from extension data to database entity.
- **Suggested test name:** `ChapterInfoHelper_ConvertsChapterInfoToDbChapter`
- **Success checks:**
  - Given ChapterInfo from extension.
  - When `ToChapter()` is called with DbManga.
  - Then DbChapter is created with new V7 UUID.
  - And MangaId is set from DbManga.
  - And Volume, Number, Title are preserved.
  - And chapter is ready to accept download links.

### 2.2 ChapterInfoHelper creates and chains download links
- **Purpose:** Verify download link attachment and chaining.
- **Suggested test name:** `ChapterInfoHelper_CreatesAndChainsDownloadLinks`
- **Success checks:**
  - Given ChapterInfo.
  - When `ToChapterDownloadLink()` is called.
  - Then DbChapterDownloadLink is created.
  - And ChapterId, DownloadExtension, Identifier, Url are preserved.
  - And Priority defaults to 0.
  - And multiple links can be chained with `CreateAndAddChapterDownloadLink()`.
  - And all links are retained.

### 2.3 MangaDTOHelper converts DbMangaMetadataEntries to DTO correctly
- **Purpose:** Verify DTO conversion for API responses.
- **Suggested test name:** `MangaDTOHelper_ConvertsToMangaDTO`
- **Success checks:**
  - Given DbMangaMetadataEntries with linked metadata.
  - When `ToDTO()` is called.
  - Then Entities.Manga is created.
  - And MangaId is preserved.
  - And Monitored status from Manga is preserved.
  - And MetadataEntry is converted and Chosen flag is set correctly.

---

## 3. API Endpoints - Manga

HTTP endpoints for manga queries and metadata management.

See `SHARED_TEST_PATTERNS.md` Section 2 for generic API endpoint patterns.

### 3.1 GET /mangas returns all manga
- **Purpose:** Verify list endpoint.
- **Suggested test name:** `GetMangaList_ReturnsAllManga`
- **Success checks:**
  - When GET `/api/mangas` is called.
  - Then response is 200 OK.
  - And all manga are returned as array.
  - And each includes MangaId and Monitored status.

### 3.2 GET /mangas returns empty array when no manga
- **Purpose:** Verify empty-state handling.
- **Suggested test name:** `GetMangaList_ReturnsEmptyWhenNoneExist`
- **Success checks:**
  - When no manga are registered and GET is called.
  - Then response is 200 OK.
  - And empty array is returned.

### 3.3 GET /mangas/{mangaId} returns specific manga
- **Purpose:** Verify single manga retrieval.
- **Suggested test name:** `GetManga_ReturnsSpecificMangaById`
- **Success checks:**
  - Given a manga with known ID.
  - When GET `/api/mangas/{mangaId}` is called.
  - Then response is 200 OK.
  - And the specific manga is returned with all fields.

### 3.4 GET /mangas/{mangaId} returns 404 for unknown ID
- **Purpose:** Verify not-found error handling.
- **Suggested test name:** `GetManga_Returns404ForUnknownId`
- **Success checks:**
  - When unknown mangaId is queried.
  - Then response is 404 Not Found.

### 3.5 GET /mangas/{mangaId}/cover returns manga cover image
- **Purpose:** Verify cover image retrieval.
- **Suggested test name:** `GetMangaCover_ReturnsCoverImage`
- **Success checks:**
  - Given a manga with a cover.
  - When GET `/api/mangas/{mangaId}/cover` is called.
  - Then response is 200 OK.
  - And image data is returned.
  - And content type indicates image (e.g., image/jpeg).

### 3.6 GET /mangas/{mangaId}/cover returns 404 when no cover
- **Purpose:** Verify missing cover handling.
- **Suggested test name:** `GetMangaCover_Returns404WhenNoCover`
- **Success checks:**
  - When manga has no cover.
  - Then response is 404 Not Found.

### 3.7 GET /mangas/{mangaId}/metadata returns manga metadata
- **Purpose:** Verify metadata retrieval for manga.
- **Suggested test name:** `GetMangaMetadata_ReturnsMetadata`
- **Success checks:**
  - Given a manga with metadata.
  - When GET is called.
  - Then response is 200 OK.
  - And metadata is returned.

### 3.8 GET /mangas/{mangaId}/metadata/related returns all related entries
- **Purpose:** Verify related metadata entries.
- **Suggested test name:** `GetMangaMetadataEntries_ReturnsAllRelatedEntries`
- **Success checks:**
  - Given a manga with multiple metadata entries.
  - When GET is called.
  - Then all related entries are returned.

### 3.9 PATCH /mangas/{mangaId}/metadata/{metadataId} sets chosen entry
- **Purpose:** Verify metadata selection.
- **Suggested test name:** `PatchMangaMetadata_SetsChosenEntry`
- **Success checks:**
  - Given a metadata entry ID.
  - When PATCH is called to mark as chosen.
  - Then response indicates success.
  - And entry is marked as source of truth.

### 3.10 PATCH /mangas/{mangaId}/metadata validates IDs
- **Purpose:** Verify input validation.
- **Suggested test name:** `PatchMangaMetadata_Validates400OnInvalidIds`
- **Success checks:**
  - When invalid mangaId or metadataId is provided.
  - Then response is 400 or 404.

### 3.11 GET /mangas/{mangaId}/downloadLinks returns all links
- **Purpose:** Verify download link retrieval.
- **Suggested test name:** `GetMangaDownloadLinks_ReturnsAllLinks`
- **Success checks:**
  - Given a manga with download links.
  - When GET is called.
  - Then all links are returned.

### 3.12 PATCH /mangas/{mangaId}/downloadLinks/{downloadId} sets priority
- **Purpose:** Verify priority setting.
- **Suggested test name:** `PatchMangaDownloadLink_SetsPriority`
- **Success checks:**
  - Given a download link ID and new priority.
  - When PATCH is called.
  - Then response indicates success.
  - And priority is updated.

---

## 4. API Endpoints - Chapters

HTTP endpoints for chapter management.

### 4.1 GET /mangas/chapters returns all chapters
- **Purpose:** Verify chapter list endpoint.
- **Suggested test name:** `GetChapters_ReturnsAllChapters`
- **Success checks:**
  - When GET `/api/mangas/chapters` is called.
  - Then all chapters are returned as array.
  - And each includes required fields (ChapterId, MangaId, Title, Volume, Number).

### 4.2 GET /mangas/chapters filters by mangaId (if supported)
- **Purpose:** Verify chapter filtering.
- **Suggested test name:** `GetChapters_FiltersByMangaIdIfSupported`
- **Success checks:**
  - Given a mangaId query parameter.
  - When GET is called with filter.
  - Then only chapters for that manga are returned.
  - (If filtering not supported, remove this test.)

### 4.3 GET /mangas/chapters/{chapterId} returns specific chapter
- **Purpose:** Verify single chapter retrieval.
- **Suggested test name:** `GetChapter_ReturnsSpecificChapterById`
- **Success checks:**
  - Given a known chapterId.
  - When GET is called.
  - Then response is 200 OK.
  - And that chapter is returned.

### 4.4 GET /mangas/chapters/{chapterId} returns 404 for unknown ID
- **Purpose:** Verify not-found error handling.
- **Suggested test name:** `GetChapter_Returns404ForUnknownId`
- **Success checks:**
  - When unknown chapterId is queried.
  - Then response is 404 Not Found.

---

## 5. API Endpoints - Metadata

HTTP endpoints for metadata extension management and discovery.

### 5.1 GET /mangas/metadata/extensions returns all extensions
- **Purpose:** Verify metadata extension list.
- **Suggested test name:** `GetMetadataExtensions_ReturnsAllExtensions`
- **Success checks:**
  - When GET is called.
  - Then all metadata extensions are returned.

### 5.2 GET /mangas/metadata returns all entries
- **Purpose:** Verify metadata entry list.
- **Suggested test name:** `GetMetadataEntries_ReturnsAllEntries`
- **Success checks:**
  - When GET is called.
  - Then all metadata entries are returned.

### 5.3 GET /mangas/metadata/{metadataId} returns specific entry
- **Purpose:** Verify single entry retrieval.
- **Suggested test name:** `GetMetadataEntry_ReturnsSpecificEntryById`
- **Success checks:**
  - Given a known metadataId.
  - When GET is called.
  - Then that entry is returned.

### 5.4 GET /mangas/metadata/{metadataId}/manga returns linked manga
- **Purpose:** Verify manga linked to metadata.
- **Suggested test name:** `GetMetadataManga_ReturnsLinkedManga`
- **Success checks:**
  - Given metadata with linked manga.
  - When GET is called.
  - Then linked manga are returned.

### 5.5 GET /mangas/metadata/{metadataId}/manga/related returns related IDs
- **Purpose:** Verify related manga ID retrieval.
- **Suggested test name:** `GetMetadataRelatedMangaIds_ReturnsRelatedIds`
- **Success checks:**
  - Given metadata with related entries.
  - When GET is called.
  - Then related manga IDs are returned.

### 5.6 POST /mangas/search searches metadata extensions
- **Purpose:** Verify metadata search endpoint.
- **Suggested test name:** `PostSearchManga_SearchesMetadataExtensions`
- **Success checks:**
  - Given a SearchQuery payload.
  - When POST is called.
  - Then metadata extensions are queried.
  - And results are returned.

### 5.7 POST /mangas/search validates input
- **Purpose:** Verify input validation.
- **Suggested test name:** `PostSearchManga_Rejects400OnInvalidInput`
- **Success checks:**
  - When invalid SearchQuery is provided.
  - Then response is 400 Bad Request.

---

## 6. API Endpoints - Download Links

HTTP endpoints for download extension and link management.

### 6.1 GET /mangas/downloadLinks/extensions returns all extensions
- **Purpose:** Verify download extension list.
- **Suggested test name:** `GetDownloadExtensions_ReturnsAllExtensions`
- **Success checks:**
  - When GET is called.
  - Then all download extensions are returned.

### 6.2 GET /mangas/downloadLinks returns all links
- **Purpose:** Verify download link list.
- **Suggested test name:** `GetDownloadLinks_ReturnsAllLinks`
- **Success checks:**
  - When GET is called.
  - Then all download links are returned.

### 6.3 GET /mangas/downloadLinks/{downloadId} returns specific link
- **Purpose:** Verify single link retrieval.
- **Suggested test name:** `GetDownloadLink_ReturnsSpecificLinkById`
- **Success checks:**
  - Given a known downloadId.
  - When GET is called.
  - Then that link is returned.

### 6.4 POST /mangas/search/{mangaId}/downloadLinks searches extensions
- **Purpose:** Verify download link search.
- **Suggested test name:** `PostSearchMangaDownloadLinks_SearchesExtensions`
- **Success checks:**
  - Given a mangaId and search query.
  - When POST is called.
  - Then download extensions are queried.
  - And results are returned.

---

## 7. API Endpoints - Files

HTTP endpoints for file retrieval.

### 7.1 GET /mangas/files/{fileId} returns file
- **Purpose:** Verify file retrieval endpoint.
- **Suggested test name:** `GetFile_ReturnsFileById`
- **Success checks:**
  - Given a known fileId.
  - When GET is called.
  - Then file data is returned.

### 7.2 GET /mangas/files/{fileId} returns 404 for unknown ID
- **Purpose:** Verify not-found handling.
- **Suggested test name:** `GetFile_Returns404ForUnknownId`
- **Success checks:**
  - When unknown fileId is queried.
  - Then response is 404 Not Found.

---

## 8. Common Endpoint Tests

See `SHARED_TEST_PATTERNS.md` Section 2.2 for generic endpoint tests that apply to all services.

---

## 9. Integration Tests

End-to-end tests combining multiple components.

### 9.1 Full workflow: Create manga, add metadata, add links
- **Purpose:** Verify end-to-end manga management workflow.
- **Suggested test name:** `Integration_MangaLifecycle_CreateMetadataAndLinks`
- **Success checks:**
  - Manga is created via endpoint.
  - Metadata is searched and a candidate is selected.
  - Download links are found and added.
  - All queries return consistent data across lifecycle.
  - Cover image handling works correctly.

### 9.2 Search and configuration affect downstream retrieval
- **Purpose:** Verify search results affect what data is available.
- **Suggested test name:** `Integration_SearchConfigurationAffectsRetrieval`
- **Success checks:**
  - Before search: no metadata/links for manga.
  - After search: metadata candidates appear.
  - After selection: chosen metadata is marked.
  - Metadata and links are accessible via GET endpoints.
  - Deleting metadata removes it from future queries.

### 9.3 Priority system affects download link ordering
- **Purpose:** Verify priority-based link selection.
- **Suggested test name:** `Integration_PriorityAffectsLinkOrdering`
- **Success checks:**
  - Multiple download links exist for manga.
  - Links are ordered by priority in responses.
  - Priority can be changed via PATCH endpoint.
  - New ordering is reflected in subsequent queries.

### 9.4 Chapter management across multiple extensions
- **Purpose:** Verify chapter deduplication and multi-source handling.
- **Suggested test name:** `Integration_ChapterManagementMultipleSources`
- **Success checks:**
  - Chapters from different download extensions are tracked separately.
  - Same chapter from different sources is handled correctly (deduplication or linking).
  - All retrieval endpoints show consistent chapter data.

---

## Recommended implementation order
1. **Entities** (1.1–1.4)
   - Foundation: data models with field constraints.
   
2. **Helpers** (2.1–2.3)
   - Conversion logic needed for endpoints.
   
3. **API Endpoints** (3–8)
   - User-facing HTTP contracts.
   - See `SHARED_TEST_PATTERNS.md` Section 2 for generic patterns.
   
4. **Integration Tests** (9.1–9.4)
   - End-to-end workflows.

## Notes

### Endpoint specific
- Use `TestServer` or `WebApplicationFactory<Program>` for integration-style tests.
- Verify persistence by querying database after endpoint calls.
- See `SHARED_TEST_PATTERNS.md` Section 2 for generic API endpoint patterns.
- Test both happy-path (200) and error-path (400, 404) for each endpoint.

### Database setup
- Use in-memory or test fixture database for all tests.
- Ensure test data cleanup between runs.
- Seed realistic test data (manga with covers, multiple chapters, various metadata).

### Integration test specifics
- Mock extension clients (metadata and download) to avoid HTTP calls.
- Test realistic user workflows (search → select → retrieve).
- Verify side effects are observable in database state.

