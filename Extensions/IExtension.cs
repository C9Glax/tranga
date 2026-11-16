namespace Extensions;

public interface IExtension
{
    /// <summary>
    /// The unique Extension Identifier
    /// </summary>
    public Guid Identifier { get; init; }
    
    /// <summary>
    /// The name of the Extension
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// The Url of the extension
    /// </summary>
    public string BaseUrl { get; init; }

    /// <summary>
    /// Url to the extension's icon/logo, shown by the frontend. Absolute for the built-in extensions; providers of
    /// dynamic extensions may use a gateway-relative path.
    /// </summary>
    public string IconUrl { get; init; }
}