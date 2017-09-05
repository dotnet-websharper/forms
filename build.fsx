#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.Forms")
        .VersionFrom("WebSharper")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net40)

let main =
    bt.WebSharper4.Library("WebSharper.Forms")
        .SourcesFromProject()
        .WithSourceMap()
        .References(fun r ->
            [
                r.NuGet("WebSharper.UI.Next").Latest(true).ForceFoundVersion().Reference()
            ])

let test =
    bt.WebSharper4.BundleWebsite("WebSharper.Forms.Tests")
        .SourcesFromProject()
        .WithSourceMap()
        .References(fun r ->
            [
                r.NuGet("WebSharper.UI.Next").Latest(true).ForceFoundVersion().Reference()
                r.Project(main)
            ])

bt.Solution [

    main
    test

    bt.NuGet.CreatePackage()
        .Description("Provides a framework to build reactive interfaces in WebSharper,\
                      similar to Formlets but with more control over the structure of the output.")
        .ProjectUrl("http://github.com/intellifactory/websharper.forms")
        .Configure(fun c ->
            {
                c with
                    Authors = ["IntelliFactory"]
                    Title = Some "WebSharper.Forms"
                    LicenseUrl = Some "http://github.com/intellifactory/websharper.forms/blob/master/LICENSE.md"
                    RequiresLicenseAcceptance = true
            })
        .Add(main)

]
|> bt.Dispatch
