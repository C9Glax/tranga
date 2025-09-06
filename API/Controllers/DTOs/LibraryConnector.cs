using System.ComponentModel.DataAnnotations;
using API.Schema.LibraryContext.LibraryConnectors;

namespace API.Controllers.DTOs;

public record LibraryConnector(string Key, string BaseUrl, LibraryType Type) : Identifiable(Key)
{
    [StringLength(256)]
    [Required]
    [Url]
    public string BaseUrl {get; init;} =  BaseUrl;

    [Required]
    public LibraryType Type { get; init; } = Type;
}