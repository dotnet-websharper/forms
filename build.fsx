#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.Forms")
        .VersionFrom("WebSharper", "alpha")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net40)

let main =
    bt.WebSharper.Library("WebSharper.Forms")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("WebSharper.UI.Next").ForceFoundVersion().Reference()
            ])

let test =
    bt.WebSharper.BundleWebsite("WebSharper.Forms.Tests")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("WebSharper.UI.Next").Reference()
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
