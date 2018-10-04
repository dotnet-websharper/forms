// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2018 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}
namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Forms
open WebSharper.Forms.Tests.Forms

[<JavaScript>]
module RenderWithoutTemplate =

    let Render() =
        Forms.FullForm()
        |> Form.Render (fun items submit ->
            div [] [
                h2 [] [text "Contacts:"]
                table [] [
                    thead [] [
                        tr [] [
                            th [] [text "Name"]
                            th [] [text "Contact"]
                        ]
                    ]
                    tbody [] [
                        items.Render (fun ops x ->
                            tr [] [
                                td [] [textView (x.View |> View.Map fst)]
                                td [] [textView (x.View |> View.Map (function
                                    | _, Email x -> "email: " + x
                                    | _, PhoneNumber x -> "phone: " + x
                                ))]
                                td [] [Doc.ButtonValidate "Move up" [] ops.MoveUp]
                                td [] [Doc.ButtonValidate "Move down" [] ops.MoveDown]
                                td [] [button [on.click (fun _ _ -> ops.Delete())] [text "Delete"]]
                            ]
                        )
                    ]
                ]
                div [attr.style "border: solid 1px #888; padding: 10px; margin: 20px"] [
                    h3 [] [text "Add a new item"]
                    items.RenderAdder (fun rvName depContact submit ->
                        div [] [
                            p [] [
                                Doc.Input [] rvName
                                ShowErrorMessage (submit.View.Through rvName)
                            ]
                            p [] [
                                depContact.RenderPrimary (fun rvContactType ->
                                    div [] [
                                        label [] [Doc.Radio [] true rvContactType; text "Email"]
                                        label [] [Doc.Radio [] false rvContactType; text "Phone number"]
                                    ]
                                )
                                depContact.RenderDependent (fun rvContact ->
                                    Doc.Concat [
                                        Doc.Input [] rvContact
                                        ShowErrorMessage (submit.View.Through rvContact)
                                    ]
                                )
                            ]
                            p [] [Doc.Button "Add" [] submit.Trigger]
                        ]
                    )
                ]
                Doc.Button "Submit" [] submit.Trigger
                submit.View |> View.Map (function
                    | Success contacts ->
                        div [] (
                            contacts |> Seq.map (fun (x, contact) ->
                                let contact =
                                    match contact with
                                    | Email e -> " (email: " + e + ")"
                                    | PhoneNumber n -> " (phone: " + n + ")"
                                p [] [text ("You registered a contact: " + string x + contact)] :> Doc
                            )
                        )
                    | Failure msgs -> div [attr.style "color:red"] [for msg in msgs -> p [] [text msg.Text] :> _]
                )
                |> Doc.EmbedView
            ]
        )
