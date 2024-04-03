# GenDocsLib
This library is related to [CSharpToJavaScript](https://github.com/TiLied/CSharpToJavaScript) for generating xml docs from MDN.

## How to use
- Download [MDN-content web-api](https://github.com/mdn/content/tree/main/files/en-us/web/api)
- Add in Main Method:
```csharp
GenDocs genDocs = new();
await genDocs.GenerateDocs("FULL PATH FOR FOLDER OF DOCS", "FULL PATH FOR OUTPUT XML FILES");
```

## Some Todos
- [x] ~Figure out "& q u o t ;" stuff~
- [x] ~Generate more MDN-content docs and not only [web-api](https://github.com/mdn/content/tree/main/files/en-us/web/api)~ ~partially completed! generated web~ Reverted, extra files not used, will be added manually in "Docs2" see: https://github.com/TiLied/GenCSharpLib/blob/6be70be394270fc1aeebc919eab4314237db4bd1/GenCSharpLib/GenCSharp.cs#L1379.
- [ ] Do members in xml, see [this](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/examples)
- [ ] How to do parameters???

## Related Repository 
CSharpToJavaScript library: https://github.com/TiLied/CSharpToJavaScript
- Library for generating csharp: https://github.com/TiLied/GenCSharpLib

CLI for library: https://github.com/TiLied/CSTOJS_CLI
  
VS Code Extension using CLI: https://github.com/TiLied/CSTOJS_VSCode_Ext

VS Extension using CLI: https://github.com/TiLied/CSTOJS_VS_Ext

Website/documentation: https://github.com/TiLied/CSTOJS_Pages
- Blazor WebAssembly: https://github.com/TiLied/CSTOJS_BWA

## Thanks for packages and content <3
[Markdig](https://github.com/xoofx/markdig) nuget package

[MDN-content](https://github.com/mdn/content) for js docs
