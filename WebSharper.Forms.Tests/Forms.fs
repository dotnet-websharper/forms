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
open WebSharper.Forms

[<JavaScript>]
module Forms =

    type Contact = Email of string | PhoneNumber of string

    let AddItemForm() =
        Form.Return (fun x y -> (x, y))
        <*> (Form.Yield "John Doe"
            |> Validation.IsNotEmpty "Please enter a name.")
        <*> Form.Do {
            let! isEmail = Form.Yield true
            if isEmail then
                return! Form.Yield "john@doe.com"
                    |> Validation.IsMatch @"^.+@.+\..+$" "Please enter a valid email address."
                    |> Form.Map Email
            else
                return! Form.Yield "01 234 5678"
                    |> Validation.Is (fun s -> s.Length >= 6) "Please enter a valid phone number."
                    |> Form.Map PhoneNumber
        }
        |> Form.WithSubmit

    let FullForm() =
        Form.ManyForm Seq.empty (AddItemForm()) Form.Yield
        |> Validation.Is (not << Seq.isEmpty) "Please enter at least one contact."
        |> Form.WithSubmit
