# Komga.Client.Api.ServerSettingsApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetServerSettings**](ServerSettingsApi.md#getserversettings) | **GET** /api/v1/settings | Retrieve server settings |
| [**UpdateServerSettings**](ServerSettingsApi.md#updateserversettings) | **PATCH** /api/v1/settings | Update server settings |

<a id="getserversettings"></a>
# **GetServerSettings**
> SettingsDto GetServerSettings ()

Retrieve server settings

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
    public class GetServerSettingsExample
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
            var apiInstance = new ServerSettingsApi(httpClient, config, httpClientHandler);

            try
            {
                // Retrieve server settings
                SettingsDto result = apiInstance.GetServerSettings();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ServerSettingsApi.GetServerSettings: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetServerSettingsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Retrieve server settings
    ApiResponse<SettingsDto> response = apiInstance.GetServerSettingsWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ServerSettingsApi.GetServerSettingsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

[**SettingsDto**](SettingsDto.md)

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

<a id="updateserversettings"></a>
# **UpdateServerSettings**
> void UpdateServerSettings (SettingsUpdateDto settingsUpdateDto)

Update server settings

You can omit fields you don't want to update  Required role: **ADMIN**

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
    public class UpdateServerSettingsExample
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
            var apiInstance = new ServerSettingsApi(httpClient, config, httpClientHandler);
            var settingsUpdateDto = new SettingsUpdateDto(); // SettingsUpdateDto | 

            try
            {
                // Update server settings
                apiInstance.UpdateServerSettings(settingsUpdateDto);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ServerSettingsApi.UpdateServerSettings: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the UpdateServerSettingsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update server settings
    apiInstance.UpdateServerSettingsWithHttpInfo(settingsUpdateDto);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ServerSettingsApi.UpdateServerSettingsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **settingsUpdateDto** | [**SettingsUpdateDto**](SettingsUpdateDto.md) |  |  |

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

