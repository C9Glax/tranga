# Komga.Client.Api.BookPagesApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetBookPageByNumber**](BookPagesApi.md#getbookpagebynumber) | **GET** /api/v1/books/{bookId}/pages/{pageNumber} | Get book page image |
| [**GetBookPageRawByNumber**](BookPagesApi.md#getbookpagerawbynumber) | **GET** /api/v1/books/{bookId}/pages/{pageNumber}/raw | Get raw book page |
| [**GetBookPageThumbnailByNumber**](BookPagesApi.md#getbookpagethumbnailbynumber) | **GET** /api/v1/books/{bookId}/pages/{pageNumber}/thumbnail | Get book page thumbnail |
| [**GetBookPages**](BookPagesApi.md#getbookpages) | **GET** /api/v1/books/{bookId}/pages | List book pages |

<a id="getbookpagebynumber"></a>
# **GetBookPageByNumber**
> FileParameter GetBookPageByNumber (string bookId, int pageNumber, string? convert = null, bool? zeroBased = null, List<MediaType>? accept = null, bool? contentNegotiation = null)

Get book page image

Required role: **PAGE_STREAMING**

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
    public class GetBookPageByNumberExample
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
            var apiInstance = new BookPagesApi(httpClient, config, httpClientHandler);
            var bookId = "bookId_example";  // string | 
            var pageNumber = 56;  // int | 
            var convert = "jpeg";  // string? | Convert the image to the provided format. (optional) 
            var zeroBased = false;  // bool? | If set to true, pages will start at index 0. If set to false, pages will start at index 1. (optional)  (default to false)
            var accept = new List<MediaType>?(); // List<MediaType>? | Some very limited server driven content negotiation is handled. If a book is a PDF book, and the Accept header contains 'application/pdf' as a more specific type than other 'image/' types, a raw PDF page will be returned. (optional) 
            var contentNegotiation = true;  // bool? |  (optional)  (default to true)

            try
            {
                // Get book page image
                FileParameter result = apiInstance.GetBookPageByNumber(bookId, pageNumber, convert, zeroBased, accept, contentNegotiation);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling BookPagesApi.GetBookPageByNumber: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookPageByNumberWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get book page image
    ApiResponse<FileParameter> response = apiInstance.GetBookPageByNumberWithHttpInfo(bookId, pageNumber, convert, zeroBased, accept, contentNegotiation);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling BookPagesApi.GetBookPageByNumberWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **bookId** | **string** |  |  |
| **pageNumber** | **int** |  |  |
| **convert** | **string?** | Convert the image to the provided format. | [optional]  |
| **zeroBased** | **bool?** | If set to true, pages will start at index 0. If set to false, pages will start at index 1. | [optional] [default to false] |
| **accept** | [**List&lt;MediaType&gt;?**](MediaType.md) | Some very limited server driven content negotiation is handled. If a book is a PDF book, and the Accept header contains &#39;application/pdf&#39; as a more specific type than other &#39;image/&#39; types, a raw PDF page will be returned. | [optional]  |
| **contentNegotiation** | **bool?** |  | [optional] [default to true] |

### Return type

[**FileParameter**](FileParameter.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*, image/*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **400** | Bad Request |  -  |
| **0** | default response |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getbookpagerawbynumber"></a>
# **GetBookPageRawByNumber**
> byte[] GetBookPageRawByNumber (string bookId, int pageNumber)

Get raw book page

Returns the book page in raw format, without content negotiation.  Required role: **PAGE_STREAMING**

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
    public class GetBookPageRawByNumberExample
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
            var apiInstance = new BookPagesApi(httpClient, config, httpClientHandler);
            var bookId = "bookId_example";  // string | 
            var pageNumber = 56;  // int | 

            try
            {
                // Get raw book page
                byte[] result = apiInstance.GetBookPageRawByNumber(bookId, pageNumber);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling BookPagesApi.GetBookPageRawByNumber: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookPageRawByNumberWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get raw book page
    ApiResponse<byte[]> response = apiInstance.GetBookPageRawByNumberWithHttpInfo(bookId, pageNumber);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling BookPagesApi.GetBookPageRawByNumberWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **bookId** | **string** |  |  |
| **pageNumber** | **int** |  |  |

### Return type

**byte[]**

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*, application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getbookpagethumbnailbynumber"></a>
# **GetBookPageThumbnailByNumber**
> FileParameter GetBookPageThumbnailByNumber (string bookId, int pageNumber)

Get book page thumbnail

The image is resized to 300px on the largest dimension.

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
    public class GetBookPageThumbnailByNumberExample
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
            var apiInstance = new BookPagesApi(httpClient, config, httpClientHandler);
            var bookId = "bookId_example";  // string | 
            var pageNumber = 56;  // int | 

            try
            {
                // Get book page thumbnail
                FileParameter result = apiInstance.GetBookPageThumbnailByNumber(bookId, pageNumber);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling BookPagesApi.GetBookPageThumbnailByNumber: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookPageThumbnailByNumberWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get book page thumbnail
    ApiResponse<FileParameter> response = apiInstance.GetBookPageThumbnailByNumberWithHttpInfo(bookId, pageNumber);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling BookPagesApi.GetBookPageThumbnailByNumberWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **bookId** | **string** |  |  |
| **pageNumber** | **int** |  |  |

### Return type

[**FileParameter**](FileParameter.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: */*, application/json, image/jpeg


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **400** | Bad Request |  -  |
| **0** | default response |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getbookpages"></a>
# **GetBookPages**
> List&lt;PageDto&gt; GetBookPages (string bookId)

List book pages

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
    public class GetBookPagesExample
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
            var apiInstance = new BookPagesApi(httpClient, config, httpClientHandler);
            var bookId = "bookId_example";  // string | 

            try
            {
                // List book pages
                List<PageDto> result = apiInstance.GetBookPages(bookId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling BookPagesApi.GetBookPages: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetBookPagesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List book pages
    ApiResponse<List<PageDto>> response = apiInstance.GetBookPagesWithHttpInfo(bookId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling BookPagesApi.GetBookPagesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **bookId** | **string** |  |  |

### Return type

[**List&lt;PageDto&gt;**](PageDto.md)

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

