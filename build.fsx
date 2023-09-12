#if INTERACTIVE
#r "nuget: FAKE.Core"
#r "nuget: Fake.Core.Target"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.Tools.Git"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.DotNet.AssemblyInfoFile"
#r "nuget: Fake.DotNet.Paket"
#r "nuget: Paket.Core"
#else
#r "paket:
nuget FSharp.Core 5.0.0
nuget FAKE.Core
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git
nuget Fake.DotNet.Cli
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Paket.Core prerelease //"
#endif

#load "paket-files/wsbuild/github.com/dotnet-websharper/build-script/WebSharper.Fake.fsx"
#r "System.Xml.Linq"

// Only reference these packages from editors/non fake-cli tools
#if !FAKE
    // To have proper language service in the editor
    #r "netstandard"
    // To help FAKE related IntelliSense in editor
    #load "./.fake/build.fsx/intellisense_lazy.fsx"
#endif

open WebSharper.Fake
open Fake.DotNet

LazyVersionFrom "WebSharper" |> WSTargets.Default
|> fun args ->
    { args with
        Attributes =
                [
                    AssemblyInfo.Company "IntelliFactory"
                    AssemblyInfo.Copyright "(c) IntelliFactory 2023"
                    AssemblyInfo.Title "https://github.com/dotnet-websharper/forms"
                    AssemblyInfo.Product "WebSharper Forms"
                ]
    }
|> MakeTargets
|> RunTargets
