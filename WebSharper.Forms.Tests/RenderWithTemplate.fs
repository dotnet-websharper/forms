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

#nowarn "58" // indentation

[<JavaScript>]
module RenderWithTemplate =

    type Template = Templating.Template<"index.html">

    let Render() =
        Forms.FullForm()
        |> Form.Render (fun items submit ->
            Template.Form()
                .Items(
                    items.Render (fun ops rvContact ->
                        Template.Item()
                            .Name(rvContact.View |> View.Map fst)
                            .Contact(rvContact.View |> View.Map (function
                                | _, Email x -> "email: " + x
                                | _, PhoneNumber x -> "phone: " + x)
                            )
                            .MoveUp(Attr.SubmitterValidate ops.MoveUp)
                            .MoveDown(Attr.SubmitterValidate ops.MoveDown)
                            .Delete(on.click (fun _ _ -> ops.Delete()))
                            .Doc()
                    )
                )
                .Adder(
                    items.RenderAdder(fun rvName depContact submit ->
                        Template.Adder()
                            .Name(rvName)
                            .NameErrors(ShowErrorMessage (submit.View.Through rvName))
                            .ContactType(
                                depContact.RenderPrimary (fun rvContactType ->
                                    Doc.Concat [
                                        label [] [Doc.Radio [] true rvContactType; text "Email"]
                                        label [] [Doc.Radio [] false rvContactType; text "Phone number"]
                                    ]
                                )
                            )
                            .Contact(
                                depContact.RenderDependent (fun rvContact ->
                                    Template.Contact()
                                        .ContactText(rvContact)
                                        .ContactErrors(ShowErrorMessage (submit.View.Through rvContact))
                                        .Doc()
                                )
                            )
                            .Add(fun _ -> submit.Trigger())
                            .Doc()
                    )
                )
                .Submit(fun _ -> submit.Trigger())
                .SubmitResults([
                    submit.View |> View.Map (function
                        | Success xs ->
                            Doc.Concat [
                                for name, contact in xs ->
                                    let contact =
                                        match contact with
                                        | PhoneNumber n -> "phone: " + n
                                        | Email e -> "email: " + e
                                    Template.SubmitSuccess().Name(name).Contact(contact).Doc()
                            ]
                        | Failure msgs ->
                            Doc.Concat [
                                for msg in msgs ->
                                    Template.SubmitError().Message(msg.Text).Doc()
                            ]
                    )
                    |> Doc.EmbedView
                ])
                .Doc()
        )

