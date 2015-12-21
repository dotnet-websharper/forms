#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("Zafir.Forms")
        .VersionFrom("Zafir")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net40)

let main =
    bt.Zafir.Library("WebSharper.Forms")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("Zafir.UI.Next").Latest(true).ForceFoundVersion().Reference()
            ])

let test =
    bt.Zafir.BundleWebsite("WebSharper.Forms.Tests")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("Zafir.UI.Next").Reference()
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
                    Title = Some "Zafir.Forms"
                    LicenseUrl = Some "http://github.com/intellifactory/websharper.forms/blob/master/LICENSE.md"
                    RequiresLicenseAcceptance = true
            })
        .Add(main)

]
|> bt.Dispatch
