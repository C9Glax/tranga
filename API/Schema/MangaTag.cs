﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Tag")]
public class MangaTag(string tag)
{
    [StringLength(64)]
    [Required]
    public string Tag { get; init; } = tag;
}