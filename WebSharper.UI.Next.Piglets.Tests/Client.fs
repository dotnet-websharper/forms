namespace WebSharper.Piglets.Next.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Piglets

[<JavaScript>]
module Client =

    type Contact = Email of string | PhoneNumber of string

    module ViewModel =

        let pAddItem() =
            Piglet.Return (fun x y -> (x, y))
            <*> (Piglet.Yield "John Doe"
                |> Validation.IsNotEmpty "Please enter a name.")
            <*> Piglet.Do {
                let! isEmail = Piglet.Yield true
                if isEmail then
                    return! Piglet.Yield "john@doe.com"
                        |> Validation.IsMatch @"^.+@.+\..+$" "Please enter a valid email address."
                        |> Piglet.Map Email
                else
                    return! Piglet.Yield "01 234 5678"
                        |> Validation.Is (fun s -> s.Length >= 6) "Please enter a valid phone number."
                        |> Piglet.Map PhoneNumber
            }
            |> Piglet.WithSubmit

        let pFull() =
            Piglet.ManyPiglet Seq.empty (pAddItem()) Piglet.Yield
            |> Validation.Is (not << Seq.isEmpty) "Please enter at least one contact."
            |> Piglet.WithSubmit

    let ShowErrorMessage v =
        v |> View.Map (function
            | Success _ -> Doc.Empty
            | Failure msgs ->
                Doc.Concat [
                    for msg in msgs ->
                        spanAttr [attr.style "color: red"] [text msg.Text] :> _
                ]
        )
        |> Doc.EmbedView

    let Test() =
        ViewModel.pFull()
        |> Piglet.Render (fun items submit ->
            div [
                h1 [text "Contacts:"]
                table [
                    thead [
                        tr [
                            th [text "Name"]
                            th [text "Contact"]
                        ]
                    ]
                    tbody [
                        items.Render (fun ops x ->
                            tr [
                                td [textView (x.View |> View.Map fst)]
                                td [textView (x.View |> View.Map (function
                                    | _, Email x -> "email: " + x
                                    | _, PhoneNumber x -> "phone: " + x
                                ))]
                                td [Doc.ButtonValidate "Move up" [] ops.MoveUp]
                                td [Doc.ButtonValidate "Move down" [] ops.MoveDown]
                                td [buttonAttr [on.click (fun _ _ -> ops.Delete())] [text "Delete"]]
                            ]
                        )
                    ]
                ]
                divAttr [attr.style "border: solid 1px #888; padding: 10px; margin: 20px"] [
                    h3 [text "Add a new item"]
                    items.RenderAdder (fun rvName csContact submit ->
                        div [
                            p [
                                Doc.Input [] rvName
                                ShowErrorMessage (submit.View.Through rvName)
                            ]
                            p [
                                csContact.Chooser (fun rvContactType ->
                                    div [
                                        label [Doc.Radio [] true rvContactType; text "Email"]
                                        label [Doc.Radio [] false rvContactType; text "Phone number"]
                                    ]
                                )
                                csContact.Choice (fun rvContact ->
                                    Doc.Concat [
                                        Doc.Input [] rvContact
                                        ShowErrorMessage (submit.View.Through rvContact)
                                    ]
                                )
                            ]
                            p [Doc.Button "Add" [] submit.Trigger]
                        ]
                    )
                ]
                Doc.Button "Submit" [] submit.Trigger
                submit.View |> View.Map (function
                    | Success contacts ->
                        div (
                            contacts |> Seq.map (fun (x, contact) ->
                                let contact =
                                    match contact with
                                    | Email e -> " (email: " + e + ")"
                                    | PhoneNumber n -> " (phone: " + n + ")"
                                p [text ("You registered a contact: " + string x + contact)] :> Doc
                            )
                        )
                    | Failure msgs -> divAttr [attr.style "color:red"] [for msg in msgs -> p [text msg.Text] :> _]
                )
                |> Doc.EmbedView
            ]
        )

    let Main =
        Console.Log("Running JavaScript Entry Point..")
        Test() |> Doc.RunById "main"
