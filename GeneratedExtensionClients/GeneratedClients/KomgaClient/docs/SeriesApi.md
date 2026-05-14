# Komga.Client.Api.SeriesApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**DeleteSeriesFile**](SeriesApi.md#deleteseriesfile) | **DELETE** /api/v1/series/{seriesId}/file | Delete series files |
| [**DownloadSeriesAsZip**](SeriesApi.md#downloadseriesaszip) | **GET** /api/v1/series/{seriesId}/file | Download series |
| [**GetBooksBySeriesId**](SeriesApi.md#getbooksbyseriesid) | **GET** /api/v1/series/{seriesId}/books | List series&#39; books |
| [**GetCollectionsBySeriesId**](SeriesApi.md#getcollectionsbyseriesid) | **GET** /api/v1/series/{seriesId}/collections | List series&#39; collections |
| [**GetSeries**](SeriesApi.md#getseries) | **POST** /api/v1/series/list | List series |
| [**GetSeriesAlphabeticalGroups**](SeriesApi.md#getseriesalphabeticalgroups) | **POST** /api/v1/series/list/alphabetical-groups | List series groups |
| [**GetSeriesAlphabeticalGroupsDeprecated**](SeriesApi.md#getseriesalphabeticalgroupsdeprecated) | **GET** /api/v1/series/alphabetical-groups | List series groups |
| [**GetSeriesById**](SeriesApi.md#getseriesbyid) | **GET** /api/v1/series/{seriesId} | Get series details |
| [**GetSeriesDeprecated**](SeriesApi.md#getseriesdeprecated) | **GET** /api/v1/series | List series |
| [**GetSeriesLatest**](SeriesApi.md#getserieslatest) | **GET** /api/v1/series/latest | List latest series |
| [**GetSeriesNew**](SeriesApi.md#getseriesnew) | **GET** /api/v1/series/new | List new series |
| [**GetSeriesUpdated**](SeriesApi.md#getseriesupdated) | **GET** /api/v1/series/updated | List updated series |
| [**MarkSeriesAsRead**](SeriesApi.md#markseriesasread) | **POST** /api/v1/series/{seriesId}/read-progress | Mark series as read |
| [**MarkSeriesAsUnread**](SeriesApi.md#markseriesasunread) | **DELETE** /api/v1/series/{seriesId}/read-progress | Mark series as unread |
| [**SeriesAnalyze**](SeriesApi.md#seriesanalyze) | **POST** /api/v1/series/{seriesId}/analyze | Analyze series |
| [**SeriesRefreshMetadata**](SeriesApi.md#seriesrefreshmetadata) | **POST** /api/v1/series/{seriesId}/metadata/refresh | Refresh series metadata |
| [**UpdateSeriesMetadata**](SeriesApi.md#updateseriesmetadata) | **PATCH** /api/v1/series/{seriesId}/metadata | Update series metadata |

<a id="deleteseriesfile"></a>
# **DeleteSeriesFile**
> void DeleteSeriesFile (string seriesId)

Delete series files

Delete all of the series' books files on disk.  Required role: **ADMIN**

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
    public class DeleteSeriesFileExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Delete series files
                apiInstance.DeleteSeriesFile(seriesId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.DeleteSeriesFile: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteSeriesFileWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete series files
    apiInstance.DeleteSeriesFileWithHttpInfo(seriesId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.DeleteSeriesFileWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="downloadseriesaszip"></a>
# **DownloadSeriesAsZip**
> Object DownloadSeriesAsZip (string seriesId)

Download series

Download the whole series as a ZIP file.  Required role: **FILE_DOWNLOAD**

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
    public class DownloadSeriesAsZipExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Download series
                Object result = apiInstance.DownloadSeriesAsZip(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.DownloadSeriesAsZip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DownloadSeriesAsZipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Download series
    ApiResponse<Object> response = apiInstance.DownloadSeriesAsZipWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.DownloadSeriesAsZipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

**Object**

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/octet-stream, */*


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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
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
                Debug.Print("Exception when calling SeriesApi.GetBooksBySeriesId: " + e.Message);
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
    Debug.Print("Exception when calling SeriesApi.GetBooksBySeriesIdWithHttpInfo: " + e.Message);
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

<a id="getcollectionsbyseriesid"></a>
# **GetCollectionsBySeriesId**
> List&lt;CollectionDto&gt; GetCollectionsBySeriesId (string seriesId)

List series' collections

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
    public class GetCollectionsBySeriesIdExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // List series' collections
                List<CollectionDto> result = apiInstance.GetCollectionsBySeriesId(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetCollectionsBySeriesId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetCollectionsBySeriesIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series' collections
    ApiResponse<List<CollectionDto>> response = apiInstance.GetCollectionsBySeriesIdWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetCollectionsBySeriesIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

[**List&lt;CollectionDto&gt;**](CollectionDto.md)

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

<a id="getseries"></a>
# **GetSeries**
> PageSeriesDto GetSeries (SeriesSearch seriesSearch, bool? unpaged = null, int? page = null, int? size = null, List<string>? sort = null)

List series

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
    public class GetSeriesExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesSearch = new SeriesSearch(); // SeriesSearch | 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 

            try
            {
                // List series
                PageSeriesDto result = apiInstance.GetSeries(seriesSearch, unpaged, page, size, sort);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeries: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesWithHttpInfo(seriesSearch, unpaged, page, size, sort);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesSearch** | [**SeriesSearch**](SeriesSearch.md) |  |  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |

### Return type

[**PageSeriesDto**](PageSeriesDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getseriesalphabeticalgroups"></a>
# **GetSeriesAlphabeticalGroups**
> List&lt;GroupCountDto&gt; GetSeriesAlphabeticalGroups (SeriesSearch seriesSearch)

List series groups

List series grouped by the first character of their sort title.

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
    public class GetSeriesAlphabeticalGroupsExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesSearch = new SeriesSearch(); // SeriesSearch | 

            try
            {
                // List series groups
                List<GroupCountDto> result = apiInstance.GetSeriesAlphabeticalGroups(seriesSearch);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeriesAlphabeticalGroups: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesAlphabeticalGroupsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series groups
    ApiResponse<List<GroupCountDto>> response = apiInstance.GetSeriesAlphabeticalGroupsWithHttpInfo(seriesSearch);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesAlphabeticalGroupsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesSearch** | [**SeriesSearch**](SeriesSearch.md) |  |  |

### Return type

[**List&lt;GroupCountDto&gt;**](GroupCountDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: application/json
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
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
                Debug.Print("Exception when calling SeriesApi.GetSeriesAlphabeticalGroupsDeprecated: " + e.Message);
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
    Debug.Print("Exception when calling SeriesApi.GetSeriesAlphabeticalGroupsDeprecatedWithHttpInfo: " + e.Message);
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

<a id="getseriesbyid"></a>
# **GetSeriesById**
> SeriesDto GetSeriesById (string seriesId)

Get series details

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
    public class GetSeriesByIdExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Get series details
                SeriesDto result = apiInstance.GetSeriesById(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeriesById: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesByIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get series details
    ApiResponse<SeriesDto> response = apiInstance.GetSeriesByIdWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesByIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

[**SeriesDto**](SeriesDto.md)

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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
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
                Debug.Print("Exception when calling SeriesApi.GetSeriesDeprecated: " + e.Message);
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
    Debug.Print("Exception when calling SeriesApi.GetSeriesDeprecatedWithHttpInfo: " + e.Message);
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

<a id="getserieslatest"></a>
# **GetSeriesLatest**
> PageSeriesDto GetSeriesLatest (List<string>? libraryId = null, bool? deleted = null, bool? oneshot = null, bool? unpaged = null, int? page = null, int? size = null)

List latest series

Return recently added or updated series.

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
    public class GetSeriesLatestExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var oneshot = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 

            try
            {
                // List latest series
                PageSeriesDto result = apiInstance.GetSeriesLatest(libraryId, deleted, oneshot, unpaged, page, size);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeriesLatest: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesLatestWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List latest series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesLatestWithHttpInfo(libraryId, deleted, oneshot, unpaged, page, size);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesLatestWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **oneshot** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |

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

<a id="getseriesnew"></a>
# **GetSeriesNew**
> PageSeriesDto GetSeriesNew (List<string>? libraryId = null, bool? deleted = null, bool? oneshot = null, bool? unpaged = null, int? page = null, int? size = null)

List new series

Return newly added series.

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
    public class GetSeriesNewExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var oneshot = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 

            try
            {
                // List new series
                PageSeriesDto result = apiInstance.GetSeriesNew(libraryId, deleted, oneshot, unpaged, page, size);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeriesNew: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesNewWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List new series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesNewWithHttpInfo(libraryId, deleted, oneshot, unpaged, page, size);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesNewWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **oneshot** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |

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

<a id="getseriesupdated"></a>
# **GetSeriesUpdated**
> PageSeriesDto GetSeriesUpdated (List<string>? libraryId = null, bool? deleted = null, bool? oneshot = null, bool? unpaged = null, int? page = null, int? size = null)

List updated series

Return recently updated series, but not newly added ones.

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
    public class GetSeriesUpdatedExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var oneshot = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 

            try
            {
                // List updated series
                PageSeriesDto result = apiInstance.GetSeriesUpdated(libraryId, deleted, oneshot, unpaged, page, size);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.GetSeriesUpdated: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesUpdatedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List updated series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesUpdatedWithHttpInfo(libraryId, deleted, oneshot, unpaged, page, size);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.GetSeriesUpdatedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **oneshot** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |

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

<a id="markseriesasread"></a>
# **MarkSeriesAsRead**
> void MarkSeriesAsRead (string seriesId)

Mark series as read

Mark all book for series as read

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
    public class MarkSeriesAsReadExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Mark series as read
                apiInstance.MarkSeriesAsRead(seriesId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.MarkSeriesAsRead: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the MarkSeriesAsReadWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Mark series as read
    apiInstance.MarkSeriesAsReadWithHttpInfo(seriesId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.MarkSeriesAsReadWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **204** | No Content |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="markseriesasunread"></a>
# **MarkSeriesAsUnread**
> void MarkSeriesAsUnread (string seriesId)

Mark series as unread

Mark all book for series as unread

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
    public class MarkSeriesAsUnreadExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Mark series as unread
                apiInstance.MarkSeriesAsUnread(seriesId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.MarkSeriesAsUnread: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the MarkSeriesAsUnreadWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Mark series as unread
    apiInstance.MarkSeriesAsUnreadWithHttpInfo(seriesId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.MarkSeriesAsUnreadWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **204** | No Content |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="seriesanalyze"></a>
# **SeriesAnalyze**
> void SeriesAnalyze (string seriesId)

Analyze series

Required role: **ADMIN**

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
    public class SeriesAnalyzeExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Analyze series
                apiInstance.SeriesAnalyze(seriesId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.SeriesAnalyze: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SeriesAnalyzeWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Analyze series
    apiInstance.SeriesAnalyzeWithHttpInfo(seriesId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.SeriesAnalyzeWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="seriesrefreshmetadata"></a>
# **SeriesRefreshMetadata**
> void SeriesRefreshMetadata (string seriesId)

Refresh series metadata

Required role: **ADMIN**

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
    public class SeriesRefreshMetadataExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Refresh series metadata
                apiInstance.SeriesRefreshMetadata(seriesId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.SeriesRefreshMetadata: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SeriesRefreshMetadataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Refresh series metadata
    apiInstance.SeriesRefreshMetadataWithHttpInfo(seriesId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.SeriesRefreshMetadataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

void (empty response body)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **202** | Accepted |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="updateseriesmetadata"></a>
# **UpdateSeriesMetadata**
> void UpdateSeriesMetadata (string seriesId, SeriesMetadataUpdateDto seriesMetadataUpdateDto)

Update series metadata

Required role: **ADMIN**

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
    public class UpdateSeriesMetadataExample
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
            var apiInstance = new SeriesApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var seriesMetadataUpdateDto = new SeriesMetadataUpdateDto(); // SeriesMetadataUpdateDto | 

            try
            {
                // Update series metadata
                apiInstance.UpdateSeriesMetadata(seriesId, seriesMetadataUpdateDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesApi.UpdateSeriesMetadata: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpdateSeriesMetadataWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update series metadata
    apiInstance.UpdateSeriesMetadataWithHttpInfo(seriesId, seriesMetadataUpdateDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesApi.UpdateSeriesMetadataWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **seriesMetadataUpdateDto** | [**SeriesMetadataUpdateDto**](SeriesMetadataUpdateDto.md) |  |  |

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

