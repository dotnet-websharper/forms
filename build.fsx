#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.UI.Next.Piglets")
        .VersionFrom("WebSharper")
        .WithFramework(fun fw -> fw.Net40)

let main =
    bt.WebSharper.Library("WebSharper.UI.Next.Piglets")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("WebSharper.UI.Next").Reference()
            ])

let test =
    bt.WebSharper.BundleWebsite("WebSharper.UI.Next.Piglets.Tests")
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
        .Description("Provides a framework to build reactive interfaces in WebSharper,
            similar to Formlets but with more control over the structure of the output.")
        .ProjectUrl("http://github.com/intellifactory/websharper.ui.next.piglets")
        .Configure(fun c ->
            {
                c with
                    Authors = ["IntelliFactory"]
                    Title = Some "WebSharper.UI.Next.Piglets"
                    LicenseUrl = Some "http://github.com/intellifactory/websharper.ui.next.piglets/blob/master/LICENSE.md"
                    RequiresLicenseAcceptance = true
            })
        .Add(main)

]
|> bt.Dispatch
