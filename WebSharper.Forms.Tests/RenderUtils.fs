namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.Forms

[<AutoOpen; JavaScript>]
module RenderUtils =

    let ShowErrorMessage v =
        v |> View.Map (function
            | Success _ -> Doc.Empty
            | Failure msgs ->
                Doc.Concat [
                    for msg in msgs do
                        yield text msg.Text
                        yield br [] :> _
                ]
        )
        |> Doc.EmbedView
