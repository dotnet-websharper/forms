namespace WebSharper.Piglets.Next.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Piglets

[<JavaScript>]
module Main =

    let Main =
        Console.Log("Running JavaScript Entry Point..")
        RenderWithTemplate.Render() |> Doc.RunById "main"
