#r "nuget: FAKE.Core"
#r "nuget: Fake.Core.Target"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.Tools.Git"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.DotNet.AssemblyInfoFile"
#r "nuget: Fake.DotNet.Paket"
#r "nuget: Paket.Core, 8.1.0-alpha004"

open Fake.Core
let execContext = Context.FakeExecutionContext.Create false "build.fsx" []
Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

#load "paket-files/wsbuild/github.com/dotnet-websharper/build-script/WebSharper.Fake.fsx"
#r "System.Xml.Linq"

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
