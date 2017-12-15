namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Forms

[<AutoOpen; JavaScript>]
module RenderUtils =

    let ShowErrorMessage v =
        v |> Doc.BindView (function
            | Success _ -> Doc.Empty
            | Failure msgs ->
                Doc.Concat [
                    for msg in msgs do
                        yield text msg.Text
                        yield br [] [] :> _
                ]
        )
