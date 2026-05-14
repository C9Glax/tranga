# Komga.Client.Api.SeriesPosterApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**AddUserUploadedSeriesThumbnail**](SeriesPosterApi.md#adduseruploadedseriesthumbnail) | **POST** /api/v1/series/{seriesId}/thumbnails | Add series poster |
| [**DeleteUserUploadedSeriesThumbnail**](SeriesPosterApi.md#deleteuseruploadedseriesthumbnail) | **DELETE** /api/v1/series/{seriesId}/thumbnails/{thumbnailId} | Delete series poster |
| [**GetSeriesThumbnail**](SeriesPosterApi.md#getseriesthumbnail) | **GET** /api/v1/series/{seriesId}/thumbnail | Get series&#39; poster image |
| [**GetSeriesThumbnailById**](SeriesPosterApi.md#getseriesthumbnailbyid) | **GET** /api/v1/series/{seriesId}/thumbnails/{thumbnailId} | Get series poster image |
| [**GetSeriesThumbnails**](SeriesPosterApi.md#getseriesthumbnails) | **GET** /api/v1/series/{seriesId}/thumbnails | List series posters |
| [**MarkSeriesThumbnailSelected**](SeriesPosterApi.md#markseriesthumbnailselected) | **PUT** /api/v1/series/{seriesId}/thumbnails/{thumbnailId}/selected | Mark series poster as selected |

<a id="adduseruploadedseriesthumbnail"></a>
# **AddUserUploadedSeriesThumbnail**
> ThumbnailSeriesDto AddUserUploadedSeriesThumbnail (string seriesId, FileParameter file, bool? selected = null)

Add series poster

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
    public class AddUserUploadedSeriesThumbnailExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var file = new System.IO.MemoryStream(System.IO.File.ReadAllBytes("/path/to/file.txt"));  // FileParameter | 
            var selected = true;  // bool? |  (optional) 

            try
            {
                // Add series poster
                ThumbnailSeriesDto result = apiInstance.AddUserUploadedSeriesThumbnail(seriesId, file, selected);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.AddUserUploadedSeriesThumbnail: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the AddUserUploadedSeriesThumbnailWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Add series poster
    ApiResponse<ThumbnailSeriesDto> response = apiInstance.AddUserUploadedSeriesThumbnailWithHttpInfo(seriesId, file, selected);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.AddUserUploadedSeriesThumbnailWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **file** | **FileParameter****FileParameter** |  |  |
| **selected** | **bool?** |  | [optional]  |

### Return type

[**ThumbnailSeriesDto**](ThumbnailSeriesDto.md)

### Authorization

[apiKey](../README.md#apiKey), [basicAuth](../README.md#basicAuth)

### HTTP request headers

 - **Content-Type**: multipart/form-data
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="deleteuseruploadedseriesthumbnail"></a>
# **DeleteUserUploadedSeriesThumbnail**
> void DeleteUserUploadedSeriesThumbnail (string seriesId, string thumbnailId)

Delete series poster

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
    public class DeleteUserUploadedSeriesThumbnailExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var thumbnailId = "thumbnailId_example";  // string | 

            try
            {
                // Delete series poster
                apiInstance.DeleteUserUploadedSeriesThumbnail(seriesId, thumbnailId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.DeleteUserUploadedSeriesThumbnail: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteUserUploadedSeriesThumbnailWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete series poster
    apiInstance.DeleteUserUploadedSeriesThumbnailWithHttpInfo(seriesId, thumbnailId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.DeleteUserUploadedSeriesThumbnailWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **thumbnailId** | **string** |  |  |

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

<a id="getseriesthumbnail"></a>
# **GetSeriesThumbnail**
> FileParameter GetSeriesThumbnail (string seriesId)

Get series' poster image

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
    public class GetSeriesThumbnailExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Get series' poster image
                FileParameter result = apiInstance.GetSeriesThumbnail(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnail: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesThumbnailWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get series' poster image
    ApiResponse<FileParameter> response = apiInstance.GetSeriesThumbnailWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnailWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

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

<a id="getseriesthumbnailbyid"></a>
# **GetSeriesThumbnailById**
> FileParameter GetSeriesThumbnailById (string seriesId, string thumbnailId)

Get series poster image

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
    public class GetSeriesThumbnailByIdExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var thumbnailId = "thumbnailId_example";  // string | 

            try
            {
                // Get series poster image
                FileParameter result = apiInstance.GetSeriesThumbnailById(seriesId, thumbnailId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnailById: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesThumbnailByIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get series poster image
    ApiResponse<FileParameter> response = apiInstance.GetSeriesThumbnailByIdWithHttpInfo(seriesId, thumbnailId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnailByIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **thumbnailId** | **string** |  |  |

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

<a id="getseriesthumbnails"></a>
# **GetSeriesThumbnails**
> List&lt;ThumbnailSeriesDto&gt; GetSeriesThumbnails (string seriesId)

List series posters

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
    public class GetSeriesThumbnailsExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // List series posters
                List<ThumbnailSeriesDto> result = apiInstance.GetSeriesThumbnails(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnails: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSeriesThumbnailsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List series posters
    ApiResponse<List<ThumbnailSeriesDto>> response = apiInstance.GetSeriesThumbnailsWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.GetSeriesThumbnailsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

[**List&lt;ThumbnailSeriesDto&gt;**](ThumbnailSeriesDto.md)

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

<a id="markseriesthumbnailselected"></a>
# **MarkSeriesThumbnailSelected**
> void MarkSeriesThumbnailSelected (string seriesId, string thumbnailId)

Mark series poster as selected

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
    public class MarkSeriesThumbnailSelectedExample
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
            var apiInstance = new SeriesPosterApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var thumbnailId = "thumbnailId_example";  // string | 

            try
            {
                // Mark series poster as selected
                apiInstance.MarkSeriesThumbnailSelected(seriesId, thumbnailId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SeriesPosterApi.MarkSeriesThumbnailSelected: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the MarkSeriesThumbnailSelectedWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Mark series poster as selected
    apiInstance.MarkSeriesThumbnailSelectedWithHttpInfo(seriesId, thumbnailId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SeriesPosterApi.MarkSeriesThumbnailSelectedWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **thumbnailId** | **string** |  |  |

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

