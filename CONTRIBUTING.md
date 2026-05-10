# Contributing

If you want to contribute, please feel free to fork and create a Pull-Request!

## General Concepts

### Extensions

An _Extension_ is an interface to an external service. It can provide metadata, download manga or both.

### Searching Manga

Searching uses [Extensions](#extensions) that provide metadata. This should allow for higher-quality metadata,
as Scanlation sites often struggle with accuracy, depth and updated information.

### Services

The **Manga**-service should handle persistent information.

The **Tasks**-service handles automated tasks, for example updating metadata and fetching chapters.

## Codestyle

- Use explicit types for your variables. This improves readability.
    - **DO**
      ```csharp
      Manga[] zyx = Object.GetAnotherThing(); //I can see that zyx is an Array, without digging through more code
      ```
    - **DO _NOT_**
      ```csharp
      var xyz = Object.GetSomething(); //What is xyz? An Array? A string? An object?
      ```
      
### Tests

Wherever possible add unit-tests (we are using xunit) for your code.
Each project has a corresponding `<name>.Tests` project.


## Adding an _Extension_

1. If you have the OpenApi-definition use `NSwagClients.Generator`
   (see [NSwagClients/Program.cs](NSwagClients/Program.cs)) to generate client code.
2. Create an _Extension_ class, extending [IDownloadExtension](Extensions/IDownloadExtension.cs) and/or 
   [IMetadataExtension](Extensions/IMetadataExtension.cs). Each _Extension_ need to have a unique `Identifier`.

   Respective examples: [Extensions/Extensions/MangaDex.cs](Extensions/Extensions/MangaDex.cs) (download and metadata) and
   [Extensions/Extensions/MangaUpdates.cs](Extensions/Extensions/MangaUpdates.cs) (just metadata)
3. Depending on the Extension:
   - Add `IDownloadExtension`s to [Extensions/DownloadExtensionsCollection.cs](Extensions/DownloadExtensionsCollection.cs)
   - Add `IMetadataExtension`s to [Extensions/MetadataExtensionsCollection.cs](Extensions/MetadataExtensionsCollection.cs)
4. **Add Tests** in [Extensions.Tests/Extensions](Extensions.Tests/Extensions):
   - For `IDownloadExtension`s extend [Extensions.Tests/ExtensionTests.cs](Extensions.Tests/DownloadExtensionTests.cs).
   - For `IMetadataExtension`s extend [Extensions.Tests/ExtensionTests.cs](Extensions.Tests/ExtensionTests.cs).