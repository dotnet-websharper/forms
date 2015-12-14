namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
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
