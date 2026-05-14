# Komga.Client.Api.CollectionSeriesApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetSeriesByCollectionId**](CollectionSeriesApi.md#getseriesbycollectionid) | **GET** /api/v1/collections/{id}/series | List collection&#39;s series |

<a id="getseriesbycollectionid"></a>
# **GetSeriesByCollectionId**
> PageSeriesDto GetSeriesByCollectionId (string id, List<string>? libraryId = null, List<string>? status = null, List<string>? readStatus = null, List<string>? publisher = null, List<string>? language = null, List<string>? genre = null, List<string>? tag = null, List<string>? ageRating = null, List<string>? releaseYear = null, bool? deleted = null, bool? complete = null, bool? unpaged = null, int? page = null, int? size = null, List<string>? author = null)

List collection's series

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
    public class GetSeriesByCollectionIdExample
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
            var apiInstance = new CollectionSeriesApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 
            var libraryId = new List<string>?(); // List<string>? |  (optional) 
            var status = new List<string>?(); // List<string>? |  (optional) 
            var readStatus = new List<string>?(); // List<string>? |  (optional) 
            var publisher = new List<string>?(); // List<string>? |  (optional) 
            var language = new List<string>?(); // List<string>? |  (optional) 
            var genre = new List<string>?(); // List<string>? |  (optional) 
            var tag = new List<string>?(); // List<string>? |  (optional) 
            var ageRating = new List<string>?(); // List<string>? |  (optional) 
            var releaseYear = new List<string>?(); // List<string>? |  (optional) 
            var deleted = true;  // bool? |  (optional) 
            var complete = true;  // bool? |  (optional) 
            var unpaged = true;  // bool? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var author = new List<string>?(); // List<string>? | Author criteria in the format: name,role. Multiple author criteria are supported. (optional) 

            try
            {
                // List collection's series
                PageSeriesDto result = apiInstance.GetSeriesByCollectionId(id, libraryId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, deleted, complete, unpaged, page, size, author);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling CollectionSeriesApi.GetSeriesByCollectionId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesByCollectionIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List collection's series
    ApiResponse<PageSeriesDto> response = apiInstance.GetSeriesByCollectionIdWithHttpInfo(id, libraryId, status, readStatus, publisher, language, genre, tag, ageRating, releaseYear, deleted, complete, unpaged, page, size, author);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling CollectionSeriesApi.GetSeriesByCollectionIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |
| **libraryId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **status** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **readStatus** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **publisher** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **language** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **genre** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **tag** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **ageRating** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **releaseYear** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **deleted** | **bool?** |  | [optional]  |
| **complete** | **bool?** |  | [optional]  |
| **unpaged** | **bool?** |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
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

