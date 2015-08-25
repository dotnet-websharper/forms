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
            <*> Piglet.Yield "John Doe"
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
//                                td [buttonAttr [on.click (fun _ _ -> ops.MoveUp())] [text "Move up"]]
//                                td [buttonAttr [on.click (fun _ _ -> ops.MoveDown())] [text "Move down"]]
                                td [buttonAttr [on.click (fun _ _ -> ops.Delete())] [text "Delete"]]
                            ]
                        )
                    ]
                ]
                h1 [text "Add a new item"]
                items.RenderAdder (fun rvName csContact submit ->
                    div [
                        Doc.Input [] rvName
                        csContact.Chooser (fun rvContactType ->
                            div [
                                label [Doc.Radio [] true rvContactType; text "Email"]
                                label [Doc.Radio [] false rvContactType; text "Phone number"]
                            ])
                        csContact.Choice (Doc.Input [])
                        Doc.ButtonValidate "Add" [] submit
                    ])
                Doc.ButtonValidate "Submit" [] submit
                submit.View |> View.Map (function
                    | Success contacts ->
                        div (
                            contacts |> Seq.map (fun (x, contact) ->
                                let contact =
                                    match contact with
                                    | Email e -> " (email: " + e + ")"
                                    | PhoneNumber n -> " (phone: " + n + ")"
                                p [text ("You registered contact: " + string x + contact)] :> Doc
                            )
                        )
                    | Failure msgs -> div (msgs |> List.map (fun msg -> p [text msg] :> Doc))
                )
                |> Doc.EmbedView
            ]
        )

    let Main =
        Console.Log("Running JavaScript Entry Point..")
        Test() |> Doc.RunById "main"
