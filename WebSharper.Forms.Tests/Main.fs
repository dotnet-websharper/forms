namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Forms

[<JavaScript>]
module Main =

#if ZAFIR
    [<SPAEntryPoint>]
    let Main() =
#else
    let Main =
#endif
        Console.Log("Running JavaScript Entry Point..")
        RenderWithTemplate.Render() |> Doc.RunById "main"
