# Komga.Client.Api.SyncPointsApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**DeleteSyncPointsForCurrentUser**](SyncPointsApi.md#deletesyncpointsforcurrentuser) | **DELETE** /api/v1/syncpoints/me | Delete all sync points |

<a id="deletesyncpointsforcurrentuser"></a>
# **DeleteSyncPointsForCurrentUser**
> void DeleteSyncPointsForCurrentUser (List<string>? keyId = null)

Delete all sync points

If an API Key ID is passed, deletes only the sync points associated with that API Key. Deleting sync points will allow a Kobo to sync from scratch upon the next sync.

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
    public class DeleteSyncPointsForCurrentUserExample
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
            var apiInstance = new SyncPointsApi(httpClient, config, httpClientHandler);
            var keyId = new List<string>?(); // List<string>? |  (optional) 

            try
            {
                // Delete all sync points
                apiInstance.DeleteSyncPointsForCurrentUser(keyId);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SyncPointsApi.DeleteSyncPointsForCurrentUser: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeleteSyncPointsForCurrentUserWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete all sync points
    apiInstance.DeleteSyncPointsForCurrentUserWithHttpInfo(keyId);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SyncPointsApi.DeleteSyncPointsForCurrentUserWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **keyId** | [**List&lt;string&gt;?**](string.md) |  | [optional]  |

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

