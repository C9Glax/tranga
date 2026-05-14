# Komga.Client.Api.FontsApi

All URIs are relative to *https://demo.komga.org*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetFontFamilyAsCss**](FontsApi.md#getfontfamilyascss) | **GET** /api/v1/fonts/resource/{fontFamily}/css | Download CSS file |
| [**GetFontFile**](FontsApi.md#getfontfile) | **GET** /api/v1/fonts/resource/{fontFamily}/{fontFile} | Download font file |
| [**GetFonts**](FontsApi.md#getfonts) | **GET** /api/v1/fonts/families | List font families |

<a id="getfontfamilyascss"></a>
# **GetFontFamilyAsCss**
> FileParameter GetFontFamilyAsCss (string fontFamily)

Download CSS file

Download a CSS file with the @font-face block for the font family. This is used by the Epub Reader to change fonts.

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
    public class GetFontFamilyAsCssExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new FontsApi(httpClient, config, httpClientHandler);
            var fontFamily = "fontFamily_example";  // string | 

            try
            {
                // Download CSS file
                FileParameter result = apiInstance.GetFontFamilyAsCss(fontFamily);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FontsApi.GetFontFamilyAsCss: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetFontFamilyAsCssWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Download CSS file
    ApiResponse<FileParameter> response = apiInstance.GetFontFamilyAsCssWithHttpInfo(fontFamily);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FontsApi.GetFontFamilyAsCssWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **fontFamily** | **string** |  |  |

### Return type

[**FileParameter**](FileParameter.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, text/css, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getfontfile"></a>
# **GetFontFile**
> FileParameter GetFontFile (string fontFamily, string fontFile)

Download font file

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
    public class GetFontFileExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://demo.komga.org";
            // create instances of HttpClient, HttpClientHandler to be reused later with different Api classes
            HttpClient httpClient = new HttpClient();
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            var apiInstance = new FontsApi(httpClient, config, httpClientHandler);
            var fontFamily = "fontFamily_example";  // string | 
            var fontFile = "fontFile_example";  // string | 

            try
            {
                // Download font file
                FileParameter result = apiInstance.GetFontFile(fontFamily, fontFile);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FontsApi.GetFontFile: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetFontFileWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Download font file
    ApiResponse<FileParameter> response = apiInstance.GetFontFileWithHttpInfo(fontFamily, fontFile);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FontsApi.GetFontFileWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **fontFamily** | **string** |  |  |
| **fontFile** | **string** |  |  |

### Return type

[**FileParameter**](FileParameter.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, */*


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getfonts"></a>
# **GetFonts**
> List&lt;string&gt; GetFonts ()

List font families

List all available font families.

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
    public class GetFontsExample
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
            var apiInstance = new FontsApi(httpClient, config, httpClientHandler);

            try
            {
                // List font families
                List<string> result = apiInstance.GetFonts();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FontsApi.GetFonts: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetFontsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List font families
    ApiResponse<List<string>> response = apiInstance.GetFontsWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FontsApi.GetFontsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

**List<string>**

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

