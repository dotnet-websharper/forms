namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Forms

[<JavaScript>]
module Main =

    [<SPAEntryPoint>]
    let Main() =
        Doc.Concat [
            h1 [] [text "Rendered with a template:"]
            RenderWithTemplate.Render()
            hr [] []
            h1 [] [text "Rendered with HTML combinators:"]
            RenderWithoutTemplate.Render()
        ]
        |> Doc.RunAppend JS.Document.Body
