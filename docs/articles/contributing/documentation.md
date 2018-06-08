# Documentation

BenchmarkDotNet uses [DocFX](https://dotnet.github.io/docfx/) as a documentation generation tool.

## Hints

* If you want to provide a link to API, you can use
    [cross references](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#different-syntax-of-cross-reference) by
    [UID](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#define-uid).
  For example,
    `[OutlierMode](xref:BenchmarkDotNet.Mathematics.OutlierMode)` and
    `@BenchmarkDotNet.Mathematics.OutlierMode`
    will be transformed to
    [OutlierMode](xref:BenchmarkDotNet.Mathematics.OutlierMode).
    
## Notes

DocFX uses the following syntax for different types of notes:

```markdown
> [!NOTE]
> <note content>
> [!TIP]
> <note content>
> [!WARNING]
> <warning content>
> [!IMPORTANT]
> <important content>
> [!Caution]
> <caution content>
```

It will be transformed to:

> [!NOTE]
> <note content>

> [!TIP]
> <note content>

> [!WARNING]
> <warning content>

> [!IMPORTANT]
> <important content>

> [!Caution]
> <caution content>

## See also

* [DocFX User Manual](https://dotnet.github.io/docfx/tutorial/docfx.exe_user_manual.html)
* [DocFX Tutorials: Links and Cross References](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html)
* [DocFX Flavored Markdown](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a#file-inclusion)