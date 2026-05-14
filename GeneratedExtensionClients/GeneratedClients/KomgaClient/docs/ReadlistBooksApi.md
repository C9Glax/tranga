# Komga.Client.Api.ReadlistBooksApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetBookSiblingNextInReadList**](ReadlistBooksApi.md#getbooksiblingnextinreadlist) | **GET** /api/v1/readlists/{id}/books/{bookId}/next | Get next book in readlist |
| [**GetBookSiblingPreviousInReadList**](ReadlistBooksApi.md#getbooksiblingpreviousinreadlist) | **GET** /api/v1/readlists/{id}/books/{bookId}/previous | Get previous book in readlist |
| [**GetBooksByReadListId**](ReadlistBooksApi.md#getbooksbyreadlistid) | **GET** /api/v1/readlists/{id}/books | List readlist&#39;s books |

<a id="getbooksiblingnextinreadlist"></a>
# **GetBookSiblingNextInReadList**
> BookDto GetBookSiblingNextInReadList (string id, string bookId)

Get next book in readlist

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
    public class GetBookSiblingNextInReadListExample
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
            var apiInstance = new ReadlistBooksApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 
            var bookId = "bookId_example";  // string | 

            try
            {
                // Get next book in readlist
                BookDto result = apiInstance.GetBookSiblingNextInReadList(id, bookId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ReadlistBooksApi.GetBookSiblingNextInReadList: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookSiblingNextInReadListWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get next book in readlist
    ApiResponse<BookDto> response = apiInstance.GetBookSiblingNextInReadListWithHttpInfo(id, bookId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ReadlistBooksApi.GetBookSiblingNextInReadListWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |
| **bookId** | **string** |  |  |

### Return type

[**BookDto**](BookDto.md)

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

<a id="getbooksiblingpreviousinreadlist"></a>
# **GetBookSiblingPreviousInReadList**
> BookDto GetBookSiblingPreviousInReadList (string id, string bookId)

Get previous book in readlist

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
    public class GetBookSiblingPreviousInReadListExample
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
            var apiInstance = new ReadlistBooksApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 
            var bookId = "bookId_example";  // string | 

            try
            {
                // Get previous book in readlist
                BookDto result = apiInstance.GetBookSiblingPreviousInReadList(id, bookId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ReadlistBooksApi.GetBookSiblingPreviousInReadList: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookSiblingPreviousInReadListWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get previous book in readlist
    ApiResponse<BookDto> response = apiInstance.GetBookSiblingPreviousInReadListWithHttpInfo(id, bookId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ReadlistBooksApi.GetBookSiblingPreviousInReadListWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |
| **bookId** | **string** |  |  |

### Return type

[**BookDto**](BookDto.md)

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

<a id="getbooksbyreadlistid"></a>
# **GetBooksByReadListId**
> PageBookDto GetBooksByReadListId (string id, List<string>? libraryId = null, List<string>? readStatus = null, List<string>? tag = null, List<string>? mediaStatus = null, bool? deleted = null, bool? unpaged = null, int? page = null, int? size = null, List<string>? author = null)

List readlist's books

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
    public class GetBooksByReadListIdExample
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
            var apiInstance = new ReadlistBooksApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var mediaStatus = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var author = new List<string>?(); // List<string>? | Author criteria in the format: name,role. Multiple author criteria are supported. (optional) 

            try
            {
                // List readlist's books
                PageBookDto result = apiInstance.GetBooksByReadListId(id, libraryId, readStatus, tag, mediaStatus, deleted, unpaged, page, size, author);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ReadlistBooksApi.GetBooksByReadListId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBooksByReadListIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List readlist's books
    ApiResponse<PageBookDto> response = apiInstance.GetBooksByReadListIdWithHttpInfo(id, libraryId, readStatus, tag, mediaStatus, deleted, unpaged, page, size, author);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ReadlistBooksApi.GetBooksByReadListIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **mediaStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
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

