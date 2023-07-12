# Documentation

BenchmarkDotNet uses [DocFX](https://dotnet.github.io/docfx/) as a documentation generation tool.

## Hints

* If you want to provide a link to API, you can use
    [cross references](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#different-syntax-of-cross-reference) by
    [UID](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#define-uid).
  For example,
    `[SimpleJobAttribute](xref:BenchmarkDotNet.Attributes.SimpleJobAttribute)` and
    `@BenchmarkDotNet.Attributes.SimpleJobAttribute`
    will be transformed to
    [SimpleJobAttribute](xref:BenchmarkDotNet.Attributes.SimpleJobAttribute).
    
## Notes

DocFX uses the [following syntax](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a#note-warningtipimportant) inside block quote for different types of notes:

```markdown
> [!NOTE]
> note content
> [!TIP]
> tip content
> [!WARNING]
> warning content
> [!IMPORTANT]
> important content
> [!Caution]
> caution content
```

It will be transformed to:

> [!NOTE]
> note content

> [!TIP]
> tip content

> [!WARNING]
> warning content

> [!IMPORTANT]
> important content

> [!Caution]
> caution content

## Building documentation locally

You can build documentation locally with the help of the `docs-build` build task:

```
build.cmd docs-build
```

## See also

* [DocFX User Manual](https://dotnet.github.io/docfx/tutorial/docfx.exe_user_manual.html)
* [DocFX Tutorials: Links and Cross References](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html)
* [DocFX Flavored Markdown](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a#file-inclusion)
