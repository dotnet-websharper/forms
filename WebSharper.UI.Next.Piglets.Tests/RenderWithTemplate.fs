namespace WebSharper.Piglets.Next.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Piglets
open WebSharper.Piglets.Next.Tests.ViewModel

[<JavaScript>]
module RenderWithTemplate =

    let [<Literal>] TemplatePath = __SOURCE_DIRECTORY__ + "/index.html"
    type Template = Templating.Template<TemplatePath>

    let Render() =
        ViewModel.FullForm()
        |> Piglet.Render (fun items submit ->
            Template.Form.Doc(
                Items = [
                    items.Render (fun ops rvContact ->
                        Template.Item.Doc(
                            Name = (rvContact.View |> View.Map fst),
                            Contact = (rvContact.View |> View.Map (function
                                | _, Email x -> "email: " + x
                                | _, PhoneNumber x -> "phone: " + x)
                            ),
                            MoveUp = [Doc.ButtonValidate "Move up" [] ops.MoveUp],
                            MoveDown = [Doc.ButtonValidate "Move down" [] ops.MoveDown],
                            Delete = [Doc.Button "Delete" [] ops.Delete]
                        )
                    )
                ],
                Adder = [
                    items.RenderAdder(fun rvName csContact submit ->
                        Template.Adder.Doc(
                            Name = rvName,
                            NameErrors = [ShowErrorMessage (submit.View.Through rvName)],
                            ContactType = [
                                csContact.Chooser(fun rvContactType ->
                                    Doc.Concat [
                                        label [Doc.Radio [] true rvContactType; text "Email"]
                                        label [Doc.Radio [] false rvContactType; text "Phone number"]
                                    ]
                                )
                            ],
                            Contact = [
                                csContact.Choice (fun rvContact ->
                                    Template.Contact.Doc(
                                        ContactText = rvContact,
                                        ContactErrors = [ShowErrorMessage (submit.View.Through rvContact)]
                                    )
                                )
                            ],
                            Add = (fun _ _ -> submit.Trigger())
                        )
                    )
                ],
                Submit = (fun _ _ -> submit.Trigger()),
                SubmitResults = [
                    submit.View |> View.Map (function
                        | Success xs ->
                            Doc.Concat [
                                for name, contact in xs ->
                                    let contact =
                                        match contact with
                                        | PhoneNumber n -> "phone: " + n
                                        | Email e -> "email: " + e
                                    Template.SubmitSuccess.Doc(Name = name, Contact = contact)
                            ]
                        | Failure msgs ->
                            Doc.Concat [
                                for msg in msgs ->
                                    Template.SubmitError.Doc(Message = msg.Text)
                            ]
                    )
                    |> Doc.EmbedView
                ]
            )
        )

