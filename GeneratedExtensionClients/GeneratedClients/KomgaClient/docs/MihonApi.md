# Komga.Client.Api.MihonApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetMihonReadProgressByReadListId**](MihonApi.md#getmihonreadprogressbyreadlistid) | **GET** /api/v1/readlists/{id}/read-progress/tachiyomi | Get readlist read progress (Mihon) |
| [**GetMihonReadProgressBySeriesId**](MihonApi.md#getmihonreadprogressbyseriesid) | **GET** /api/v2/series/{seriesId}/read-progress/tachiyomi | Get series read progress (Mihon) |
| [**UpdateMihonReadProgressByReadListId**](MihonApi.md#updatemihonreadprogressbyreadlistid) | **PUT** /api/v1/readlists/{id}/read-progress/tachiyomi | Update readlist read progress (Mihon) |
| [**UpdateMihonReadProgressBySeriesId**](MihonApi.md#updatemihonreadprogressbyseriesid) | **PUT** /api/v2/series/{seriesId}/read-progress/tachiyomi | Update series read progress (Mihon) |

<a id="getmihonreadprogressbyreadlistid"></a>
# **GetMihonReadProgressByReadListId**
> TachiyomiReadProgressDto GetMihonReadProgressByReadListId (string id)

Get readlist read progress (Mihon)

Mihon specific, due to how read progress is handled in Mihon.

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
    public class GetMihonReadProgressByReadListIdExample
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
            var apiInstance = new MihonApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 

            try
            {
                // Get readlist read progress (Mihon)
                TachiyomiReadProgressDto result = apiInstance.GetMihonReadProgressByReadListId(id);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MihonApi.GetMihonReadProgressByReadListId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMihonReadProgressByReadListIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get readlist read progress (Mihon)
    ApiResponse<TachiyomiReadProgressDto> response = apiInstance.GetMihonReadProgressByReadListIdWithHttpInfo(id);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MihonApi.GetMihonReadProgressByReadListIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |

### Return type

[**TachiyomiReadProgressDto**](TachiyomiReadProgressDto.md)

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

<a id="getmihonreadprogressbyseriesid"></a>
# **GetMihonReadProgressBySeriesId**
> TachiyomiReadProgressV2Dto GetMihonReadProgressBySeriesId (string seriesId)

Get series read progress (Mihon)

Mihon specific, due to how read progress is handled in Mihon.

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
    public class GetMihonReadProgressBySeriesIdExample
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
            var apiInstance = new MihonApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 

            try
            {
                // Get series read progress (Mihon)
                TachiyomiReadProgressV2Dto result = apiInstance.GetMihonReadProgressBySeriesId(seriesId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MihonApi.GetMihonReadProgressBySeriesId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMihonReadProgressBySeriesIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get series read progress (Mihon)
    ApiResponse<TachiyomiReadProgressV2Dto> response = apiInstance.GetMihonReadProgressBySeriesIdWithHttpInfo(seriesId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MihonApi.GetMihonReadProgressBySeriesIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |

### Return type

[**TachiyomiReadProgressV2Dto**](TachiyomiReadProgressV2Dto.md)

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

<a id="updatemihonreadprogressbyreadlistid"></a>
# **UpdateMihonReadProgressByReadListId**
> void UpdateMihonReadProgressByReadListId (string id, TachiyomiReadProgressUpdateDto tachiyomiReadProgressUpdateDto)

Update readlist read progress (Mihon)

Mihon specific, due to how read progress is handled in Mihon.

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
    public class UpdateMihonReadProgressByReadListIdExample
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
            var apiInstance = new MihonApi(httpClient, config, httpClientHandler);
            var id = "id_example";  // string | 
            var tachiyomiReadProgressUpdateDto = new TachiyomiReadProgressUpdateDto(); // TachiyomiReadProgressUpdateDto | 

            try
            {
                // Update readlist read progress (Mihon)
                apiInstance.UpdateMihonReadProgressByReadListId(id, tachiyomiReadProgressUpdateDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MihonApi.UpdateMihonReadProgressByReadListId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpdateMihonReadProgressByReadListIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update readlist read progress (Mihon)
    apiInstance.UpdateMihonReadProgressByReadListIdWithHttpInfo(id, tachiyomiReadProgressUpdateDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MihonApi.UpdateMihonReadProgressByReadListIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **string** |  |  |
| **tachiyomiReadProgressUpdateDto** | [**TachiyomiReadProgressUpdateDto**](TachiyomiReadProgressUpdateDto.md) |  |  |

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

<a id="updatemihonreadprogressbyseriesid"></a>
# **UpdateMihonReadProgressBySeriesId**
> void UpdateMihonReadProgressBySeriesId (string seriesId, TachiyomiReadProgressUpdateV2Dto tachiyomiReadProgressUpdateV2Dto)

Update series read progress (Mihon)

Mihon specific, due to how read progress is handled in Mihon.

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
    public class UpdateMihonReadProgressBySeriesIdExample
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
            var apiInstance = new MihonApi(httpClient, config, httpClientHandler);
            var seriesId = "seriesId_example";  // string | 
            var tachiyomiReadProgressUpdateV2Dto = new TachiyomiReadProgressUpdateV2Dto(); // TachiyomiReadProgressUpdateV2Dto | 

            try
            {
                // Update series read progress (Mihon)
                apiInstance.UpdateMihonReadProgressBySeriesId(seriesId, tachiyomiReadProgressUpdateV2Dto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling MihonApi.UpdateMihonReadProgressBySeriesId: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpdateMihonReadProgressBySeriesIdWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update series read progress (Mihon)
    apiInstance.UpdateMihonReadProgressBySeriesIdWithHttpInfo(seriesId, tachiyomiReadProgressUpdateV2Dto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling MihonApi.UpdateMihonReadProgressBySeriesIdWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **seriesId** | **string** |  |  |
| **tachiyomiReadProgressUpdateV2Dto** | [**TachiyomiReadProgressUpdateV2Dto**](TachiyomiReadProgressUpdateV2Dto.md) |  |  |

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

