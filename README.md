# GenDocsLib
This library is related to [CSharpToJavaScript](https://github.com/TiLied/CSharpToJavaScript) for generating xml docs from MDN.

## How to use
- Download [MDN-content web api](https://github.com/mdn/content/tree/main/files/en-us/web/api)
- Add in Main Method:
```csharp
GenDocs genDocs = new();
genDocs.GenerateDocs("FULL PATH FOR FOLDER OF DOCS", "FULL PATH FOR OUTPUT XML FILE");
```

## Some Todos
- [ ] Figure out "& q u o t ;" stuff
- [ ] Generate more MDN-content docs and not only [web-api](https://github.com/mdn/content/tree/main/files/en-us/web/api)
- [ ] Do members in xml, see [this](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/examples)
- [ ] How to do parameters???

## Related Repository 
https://github.com/TiLied/CSharpToJavaScript

https://github.com/TiLied/GenCSharpLib

## Thanks and usings
[Markdig](https://github.com/xoofx/markdig) nuget package

[MDN-content](https://github.com/mdn/content) for js docs
