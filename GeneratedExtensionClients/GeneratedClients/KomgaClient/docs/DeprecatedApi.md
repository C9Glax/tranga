# Komga.Client.Api.DeprecatedApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetAllBooksDeprecated**](DeprecatedApi.md#getallbooksdeprecated) | **GET** /api/v1/books | List books |
| [**GetAuthorsDeprecated**](DeprecatedApi.md#getauthorsdeprecated) | **GET** /api/v1/authors | List authors |
| [**GetBooksBySeriesId**](DeprecatedApi.md#getbooksbyseriesid) | **GET** /api/v1/series/{seriesId}/books | List series&#39; books |
| [**GetSeriesAlphabeticalGroupsDeprecated**](DeprecatedApi.md#getseriesalphabeticalgroupsdeprecated) | **GET** /api/v1/series/alphabetical-groups | List series groups |
| [**GetSeriesDeprecated**](DeprecatedApi.md#getseriesdeprecated) | **GET** /api/v1/series | List series |
| [**UpdateLibraryByIdDeprecated**](DeprecatedApi.md#updatelibrarybyiddeprecated) | **PUT** /api/v1/libraries/{libraryId} | Update a library |

<a id="getallbooksdeprecated"></a>
# **GetAllBooksDeprecated**
> PageBookDto GetAllBooksDeprecated (string? search = null, List<string>? libraryId = null, List<string>? mediaStatus = null, List<string>? readStatus = null, DateOnly? releasedAfter = null, List<string>? tag = null, bool? unpaged = null, int? page = null, int? size = null, List<string>? sort = null)

List books

Use POST /api/v1/books/list instead. Deprecated since 1.19.0.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class GetAllBooksDeprecatedExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var search = "search_example";  // string? |  (optional) 
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var mediaStatus = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var releasedAfter = DateOnly.Parse("2013-10-20");  // DateOnly? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 

            try
            {
                // List books
                PageBookDto result = apiInstance.GetAllBooksDeprecated(search, libraryId, mediaStatus, readStatus, releasedAfter, tag, unpaged, page, size, sort);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.GetAllBooksDeprecated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetAllBooksDeprecatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List books
    ApiResponse<PageBookDto> response = apiInstance.GetAllBooksDeprecatedWithHttpInfo(search, libraryId, mediaStatus, readStatus, releasedAfter, tag, unpaged, page, size, sort);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.GetAllBooksDeprecatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **search** | **string?** |  | [optional]  |
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **mediaStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **releasedAfter** | **DateOnly?** |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |

### Return type

[**PageBookDto**](PageBookDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getauthorsdeprecated"></a>
# **GetAuthorsDeprecated**
> List&lt;AuthorDto&gt; GetAuthorsDeprecated (string? search = null, string? libraryId = null, string? collectionId = null, string? seriesId = null)

List authors

Use GET /api/v2/authors instead. Deprecated since 1.20.0.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class GetAuthorsDeprecatedExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var search = "\"\"";  // string? |  (optional)  (default to "")
            var libraryId = "libraryId_example";  // string? |  (optional) 
            var collectionId = "collectionId_example";  // string? |  (optional) 
            var seriesId = "seriesId_example";  // string? |  (optional) 

            try
            {
                // List authors
                List<AuthorDto> result = apiInstance.GetAuthorsDeprecated(search, libraryId, collectionId, seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.GetAuthorsDeprecated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetAuthorsDeprecatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List authors
    ApiResponse<List<AuthorDto>> response = apiInstance.GetAuthorsDeprecatedWithHttpInfo(search, libraryId, collectionId, seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.GetAuthorsDeprecatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **search** | **string?** |  | [optional] [default to &quot;&quot;] |
| **libraryId** | **string?** |  | [optional]  |
| **collectionId** | **string?** |  | [optional]  |
| **seriesId** | **string?** |  | [optional]  |

### Return type

[**List&lt;AuthorDto&gt;**](AuthorDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getbooksbyseriesid"></a>
# **GetBooksBySeriesId**
> PageBookDto GetBooksBySeriesId (string seriesId, List<string>? mediaStatus = null, List<string>? readStatus = null, List<string>? tag = null, bool? deleted = null, bool? unpaged = null, int? page = null, int? size = null, List<string>? sort = null, List<string>? author = null)

List series' books

Use POST /api/v1/books/list instead. Deprecated since 1.19.0.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class GetBooksBySeriesIdExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var mediaStatus = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 
            var author = new List<string>?(); // List<string>? | Author criteria in the format: name,role. Multiple author criteria are supported. (optional) 

            try
            {
                // List series' books
                PageBookDto result = apiInstance.GetBooksBySeriesId(seriesId, mediaStatus, readStatus, tag, deleted, unpaged, page, size, sort, author);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.GetBooksBySeriesId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBooksBySeriesIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series' books
    ApiResponse<PageBookDto> response = apiInstance.GetBooksBySeriesIdWithHttpInfo(seriesId, mediaStatus, readStatus, tag, deleted, unpaged, page, size, sort, author);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.GetBooksBySeriesIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **mediaStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |
| **author** | [**List&lt;string&gt;?**](string.md) | Author criteria in the format: name,role. Multiple author criteria are supported. | [optional]  |

### Return type

[**PageBookDto**](PageBookDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getseriesalphabeticalgroupsdeprecated"></a>
# **GetSeriesAlphabeticalGroupsDeprecated**
> List&lt;GroupCountDto&gt; GetSeriesAlphabeticalGroupsDeprecated (string? search = null, List<string>? libraryId = null, List<string>? collectionId = null, List<string>? status = null, List<string>? readStatus = null, List<string>? publisher = null, List<string>? language = null, List<string>? genre = null, List<string>? tag = null, List<string>? ageRating = null, List<string>? releaseYear = null, List<string>? sharingLabel = null, bool? deleted = null, bool? complete = null, bool? oneshot = null, string? searchRegex = null, List<string>? author = null)

List series groups

Use POST /api/v1/series/list/alphabetical-groups instead. Deprecated since 1.19.0.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class GetSeriesAlphabeticalGroupsDeprecatedExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var search = "search_example";  // string? |  (optional) 
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var collectionId = new List<string>?(); // List<string>? |  (optional) 
            var status = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var publisher = new List<string>?(); // List<string>? |  (optional) 
            var language = new List<string>?(); // List<string>? |  (optional) 
            var genre = new List<string>?(); // List<string>? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var ageRating = new List<string>?(); // List<string>? |  (optional) 
            var releaseYear = new List<string>?(); // List<string>? |  (optional) 
            var sharingLabel = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var complete = true;  // bool? |  (optional) 
            var oneshot = true;  // bool? |  (optional) 
            var searchRegex = "searchRegex_example";  // string? | Search by regex criteria, in the form: regex,field. Supported fields are TITLE and TITLE_SORT. (optional) 
            var author = new List<string>?(); // List<string>? | Author criteria in the format: name,role. Multiple author criteria are supported. (optional) 

            try
            {
                // List series groups
                List<GroupCountDto> result = apiInstance.GetSeriesAlphabeticalGroupsDeprecated(search, libraryId, collectionId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, sharingLabel, deleted, complete, oneshot, searchRegex, author);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.GetSeriesAlphabeticalGroupsDeprecated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesAlphabeticalGroupsDeprecatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series groups
    ApiResponse<List<GroupCountDto>> response = apiInstance.GetSeriesAlphabeticalGroupsDeprecatedWithHttpInfo(search, libraryId, collectionId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, sharingLabel, deleted, complete, oneshot, searchRegex, author);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.GetSeriesAlphabeticalGroupsDeprecatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **search** | **string?** |  | [optional]  |
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **collectionId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **status** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **publisher** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **language** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **genre** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **ageRating** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **releaseYear** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **sharingLabel** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **complete** | **bool?** |  | [optional]  |
| **oneshot** | **bool?** |  | [optional]  |
| **searchRegex** | **string?** | Search by regex criteria, in the form: regex,field. Supported fields are TITLE and TITLE_SORT. | [optional]  |
| **author** | [**List&lt;string&gt;?**](string.md) | Author criteria in the format: name,role. Multiple author criteria are supported. | [optional]  |

### Return type

[**List&lt;GroupCountDto&gt;**](GroupCountDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getseriesdeprecated"></a>
# **GetSeriesDeprecated**
> PageSeriesDto GetSeriesDeprecated (string? search = null, List<string>? libraryId = null, List<string>? collectionId = null, List<string>? status = null, List<string>? readStatus = null, List<string>? publisher = null, List<string>? language = null, List<string>? genre = null, List<string>? tag = null, List<string>? ageRating = null, List<string>? releaseYear = null, List<string>? sharingLabel = null, bool? deleted = null, bool? complete = null, bool? oneshot = null, bool? unpaged = null, string? searchRegex = null, int? page = null, int? size = null, List<string>? sort = null, List<string>? author = null)

List series

Use POST /api/v1/series/list instead. Deprecated since 1.19.0.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class GetSeriesDeprecatedExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var search = "search_example";  // string? |  (optional) 
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var collectionId = new List<string>?(); // List<string>? |  (optional) 
            var status = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var publisher = new List<string>?(); // List<string>? |  (optional) 
            var language = new List<string>?(); // List<string>? |  (optional) 
            var genre = new List<string>?(); // List<string>? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var ageRating = new List<string>?(); // List<string>? |  (optional) 
            var releaseYear = new List<string>?(); // List<string>? |  (optional) 
            var sharingLabel = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var complete = true;  // bool? |  (optional) 
            var oneshot = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var searchRegex = "searchRegex_example";  // string? | Search by regex criteria, in the form: regex,field. Supported fields are TITLE and TITLE_SORT. (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 
            var author = new List<string>?(); // List<string>? | Author criteria in the format: name,role. Multiple author criteria are supported. (optional) 

            try
            {
                // List series
                PageSeriesDto result = apiInstance.GetSeriesDeprecated(search, libraryId, collectionId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, sharingLabel, deleted, complete, oneshot, unpaged, searchRegex, page, size, sort, author);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.GetSeriesDeprecated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesDeprecatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesDeprecatedWithHttpInfo(search, libraryId, collectionId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, sharingLabel, deleted, complete, oneshot, unpaged, searchRegex, page, size, sort, author);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.GetSeriesDeprecatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **search** | **string?** |  | [optional]  |
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **collectionId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **status** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **publisher** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **language** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **genre** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **ageRating** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **releaseYear** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **sharingLabel** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **complete** | **bool?** |  | [optional]  |
| **oneshot** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **searchRegex** | **string?** | Search by regex criteria, in the form: regex,field. Supported fields are TITLE and TITLE_SORT. | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |
| **author** | [**List&lt;string&gt;?**](string.md) | Author criteria in the format: name,role. Multiple author criteria are supported. | [optional]  |

### Return type

[**PageSeriesDto**](PageSeriesDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="updatelibrarybyiddeprecated"></a>
# **UpdateLibraryByIdDeprecated**
> void UpdateLibraryByIdDeprecated (string libraryId, LibraryUpdateDto libraryUpdateDto)

Update a library

Use PATCH /api/v1/libraries/{libraryId} instead. Deprecated since 1.3.0.  Required role: **ADMIN**

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Example
{
    public class UpdateLibraryByIdDeprecatedExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // Configure API key authorization: apiKey
            config.AddApiKey("X-API-Key", "YOUR_API_KEY");
            // Uncomment below to setup prefix (e.g. Bearer) for API key, if needed
            // config.AddApiKeyPrefix("X-API-Key", "Bearer");
            // Configure HTTP basic authorization: basicAuth
            config.Username = "YOUR_USERNAME";
            config.Password = "YOUR_PASSWORD";

            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new DeprecatedApi(httpClient, config, httpClientHandler);
            var libraryId = "libraryId_example";  // string | 
            var libraryUpdateDto = new LibraryUpdateDto(); // LibraryUpdateDto | 

            try
            {
                // Update a library
                apiInstance.UpdateLibraryByIdDeprecated(libraryId, libraryUpdateDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DeprecatedApi.UpdateLibraryByIdDeprecated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpdateLibraryByIdDeprecatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update a library
    apiInstance.UpdateLibraryByIdDeprecatedWithHttpInfo(libraryId, libraryUpdateDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DeprecatedApi.UpdateLibraryByIdDeprecatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **libraryId** | **string** |  |  |
| **libraryUpdateDto** | [**LibraryUpdateDto**](LibraryUpdateDto.md) |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **204** | No Content |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

