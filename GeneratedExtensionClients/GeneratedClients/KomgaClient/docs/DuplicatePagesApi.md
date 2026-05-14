# Komga.Client.Api.DuplicatePagesApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreateOrUpdateKnownPageHash**](DuplicatePagesApi.md#createorupdateknownpagehash) | **PUT** /api/v1/page-hashes | Mark duplicate page as known |
| [**DeleteDuplicatePagesByPageHash**](DuplicatePagesApi.md#deleteduplicatepagesbypagehash) | **POST** /api/v1/page-hashes/{pageHash}/delete-all | Delete all duplicate pages by hash |
| [**DeleteSingleMatchByPageHash**](DuplicatePagesApi.md#deletesinglematchbypagehash) | **POST** /api/v1/page-hashes/{pageHash}/delete-match | Delete specific duplicate page |
| [**GetKnownPageHashThumbnail**](DuplicatePagesApi.md#getknownpagehashthumbnail) | **GET** /api/v1/page-hashes/{pageHash}/thumbnail | Get known duplicate image thumbnail |
| [**GetKnownPageHashes**](DuplicatePagesApi.md#getknownpagehashes) | **GET** /api/v1/page-hashes | List known duplicates |
| [**GetPageHashMatches**](DuplicatePagesApi.md#getpagehashmatches) | **GET** /api/v1/page-hashes/{pageHash} | List duplicate matches |
| [**GetUnknownPageHashThumbnail**](DuplicatePagesApi.md#getunknownpagehashthumbnail) | **GET** /api/v1/page-hashes/unknown/{pageHash}/thumbnail | Get unknown duplicate image thumbnail |
| [**GetUnknownPageHashes**](DuplicatePagesApi.md#getunknownpagehashes) | **GET** /api/v1/page-hashes/unknown | List unknown duplicates |

<a id="createorupdateknownpagehash"></a>
# **CreateOrUpdateKnownPageHash**
> void CreateOrUpdateKnownPageHash (PageHashCreationDto pageHashCreationDto)

Mark duplicate page as known

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
    public class CreateOrUpdateKnownPageHashExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHashCreationDto = new PageHashCreationDto(); // PageHashCreationDto | 

            try
            {
                // Mark duplicate page as known
                apiInstance.CreateOrUpdateKnownPageHash(pageHashCreationDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.CreateOrUpdateKnownPageHash: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateOrUpdateKnownPageHashWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Mark duplicate page as known
    apiInstance.CreateOrUpdateKnownPageHashWithHttpInfo(pageHashCreationDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.CreateOrUpdateKnownPageHashWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHashCreationDto** | [**PageHashCreationDto**](PageHashCreationDto.md) |  |  |

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
| **202** | Accepted |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="deleteduplicatepagesbypagehash"></a>
# **DeleteDuplicatePagesByPageHash**
> void DeleteDuplicatePagesByPageHash (string pageHash)

Delete all duplicate pages by hash

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
    public class DeleteDuplicatePagesByPageHashExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHash = "pageHash_example";  // string | 

            try
            {
                // Delete all duplicate pages by hash
                apiInstance.DeleteDuplicatePagesByPageHash(pageHash);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.DeleteDuplicatePagesByPageHash: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteDuplicatePagesByPageHashWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete all duplicate pages by hash
    apiInstance.DeleteDuplicatePagesByPageHashWithHttpInfo(pageHash);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.DeleteDuplicatePagesByPageHashWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHash** | **string** |  |  |

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

<a id="deletesinglematchbypagehash"></a>
# **DeleteSingleMatchByPageHash**
> void DeleteSingleMatchByPageHash (string pageHash, PageHashMatchDto pageHashMatchDto)

Delete specific duplicate page

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
    public class DeleteSingleMatchByPageHashExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHash = "pageHash_example";  // string | 
            var pageHashMatchDto = new PageHashMatchDto(); // PageHashMatchDto | 

            try
            {
                // Delete specific duplicate page
                apiInstance.DeleteSingleMatchByPageHash(pageHash, pageHashMatchDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.DeleteSingleMatchByPageHash: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteSingleMatchByPageHashWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete specific duplicate page
    apiInstance.DeleteSingleMatchByPageHashWithHttpInfo(pageHash, pageHashMatchDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.DeleteSingleMatchByPageHashWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHash** | **string** |  |  |
| **pageHashMatchDto** | [**PageHashMatchDto**](PageHashMatchDto.md) |  |  |

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
| **202** | Accepted |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getknownpagehashthumbnail"></a>
# **GetKnownPageHashThumbnail**
> FileParameter GetKnownPageHashThumbnail (string pageHash)

Get known duplicate image thumbnail

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
    public class GetKnownPageHashThumbnailExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHash = "pageHash_example";  // string | 

            try
            {
                // Get known duplicate image thumbnail
                FileParameter result = apiInstance.GetKnownPageHashThumbnail(pageHash);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.GetKnownPageHashThumbnail: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetKnownPageHashThumbnailWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get known duplicate image thumbnail
    ApiResponse<FileParameter> response = apiInstance.GetKnownPageHashThumbnailWithHttpInfo(pageHash);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.GetKnownPageHashThumbnailWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHash** | **string** |  |  |

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

<a id="getknownpagehashes"></a>
# **GetKnownPageHashes**
> PagePageHashKnownDto GetKnownPageHashes (List<string>? action = null, int? page = null, int? size = null, List<string>? sort = null)

List known duplicates

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
    public class GetKnownPageHashesExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var action = new List<string>?(); // List<string>? |  (optional) 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 

            try
            {
                // List known duplicates
                PagePageHashKnownDto result = apiInstance.GetKnownPageHashes(action, page, size, sort);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.GetKnownPageHashes: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetKnownPageHashesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List known duplicates
    ApiResponse<PagePageHashKnownDto> response = apiInstance.GetKnownPageHashesWithHttpInfo(action, page, size, sort);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.GetKnownPageHashesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **action** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |

### Return type

[**PagePageHashKnownDto**](PagePageHashKnownDto.md)

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

<a id="getpagehashmatches"></a>
# **GetPageHashMatches**
> PagePageHashMatchDto GetPageHashMatches (string pageHash, int? page = null, int? size = null, List<string>? sort = null)

List duplicate matches

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
    public class GetPageHashMatchesExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHash = "pageHash_example";  // string | 
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 

            try
            {
                // List duplicate matches
                PagePageHashMatchDto result = apiInstance.GetPageHashMatches(pageHash, page, size, sort);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.GetPageHashMatches: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetPageHashMatchesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List duplicate matches
    ApiResponse<PagePageHashMatchDto> response = apiInstance.GetPageHashMatchesWithHttpInfo(pageHash, page, size, sort);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.GetPageHashMatchesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHash** | **string** |  |  |
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |

### Return type

[**PagePageHashMatchDto**](PagePageHashMatchDto.md)

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

<a id="getunknownpagehashthumbnail"></a>
# **GetUnknownPageHashThumbnail**
> FileParameter GetUnknownPageHashThumbnail (string pageHash, int? resize = null)

Get unknown duplicate image thumbnail

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
    public class GetUnknownPageHashThumbnailExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var pageHash = "pageHash_example";  // string | 
            var resize = 56;  // int? |  (optional) 

            try
            {
                // Get unknown duplicate image thumbnail
                FileParameter result = apiInstance.GetUnknownPageHashThumbnail(pageHash, resize);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.GetUnknownPageHashThumbnail: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetUnknownPageHashThumbnailWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get unknown duplicate image thumbnail
    ApiResponse<FileParameter> response = apiInstance.GetUnknownPageHashThumbnailWithHttpInfo(pageHash, resize);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.GetUnknownPageHashThumbnailWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageHash** | **string** |  |  |
| **resize** | **int?** |  | [optional]  |

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

<a id="getunknownpagehashes"></a>
# **GetUnknownPageHashes**
> PagePageHashUnknownDto GetUnknownPageHashes (int? page = null, int? size = null, List<string>? sort = null)

List unknown duplicates

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
    public class GetUnknownPageHashesExample
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
            var apiInstance = new DuplicatePagesApi(httpClient, config, httpClientHandler);
            var page = 56;  // int? | Zero-based page index (0..N) (optional) 
            var size = 56;  // int? | The size of the page to be returned (optional) 
            var sort = new List<string>?(); // List<string>? | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. (optional) 

            try
            {
                // List unknown duplicates
                PagePageHashUnknownDto result = apiInstance.GetUnknownPageHashes(page, size, sort);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DuplicatePagesApi.GetUnknownPageHashes: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetUnknownPageHashesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List unknown duplicates
    ApiResponse<PagePageHashUnknownDto> response = apiInstance.GetUnknownPageHashesWithHttpInfo(page, size, sort);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DuplicatePagesApi.GetUnknownPageHashesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **page** | **int?** | Zero-based page index (0..N) | [optional]  |
| **size** | **int?** | The size of the page to be returned | [optional]  |
| **sort** | [**List&lt;string&gt;?**](string.md) | Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported. | [optional]  |

### Return type

[**PagePageHashUnknownDto**](PagePageHashUnknownDto.md)

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

