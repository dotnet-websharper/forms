namespace WebSharper.Forms.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.Forms

[<JavaScript>]
module ViewModel =

    type Contact = Email of string | PhoneNumber of string

    let AddItemForm() =
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

    let FullForm() =
        Piglet.ManyPiglet Seq.empty (AddItemForm()) Piglet.Yield
        |> Validation.Is (not << Seq.isEmpty) "Please enter at least one contact."
        |> Piglet.WithSubmit
