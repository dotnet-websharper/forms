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
