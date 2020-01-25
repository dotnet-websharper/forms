# Introduction to Forms

The WebSharper.Forms (aka. UI.Piglets or **reactive** piglets/forms, see also ordinary [piglets](https://github.com/dotnet-websharper/piglets)) library provides a high-level abstraction for working with web forms and constructing interactive user interfaces. It is closely related to [formlets](http://websharper.com/docs/formlets) (see also [UI.Formlets](https://github.com/dotnet-websharper/ui.formlets)) as they both provide the capability to declaratively describe user data input such as forms, including data validation and feedback. The main difference comes from the way actual widgets are rendered: Formlets automatically generate input fields and layout markup, while reactive forms let the developer render the composed form using custom markup.

You should use Piglets/Forms in one of these cases:

* You want to have absolute control over the rendering of the form.

* You are creating an application for different frontends (e.g. a web version using JQueryUI and a mobile version using JQueryMobile) and would like to factor the data definition and validation code, leaving only the actual rendering to be differenciated.

You should use Formlets in one of these cases:

* You want your code to be extremely concise and define at the same time how input data is composed and validated, and how input fields are rendered.

* You are developing a back-office application, prioritizing speed of development over pixel-perfect design.

In all cases, Piglets/Forms and Formlets have a lot in common:

* They are type-safe: unlike many "strongly typed" templating engines, in which a field is only identified by an id and there is no way to check that, say, a datepicker is indeed associated with a field of type `Date`, Formlets and Piglets/Forms are able to guarantee such properties.

* Data composition and validation is done declaratively, and the dynamic aspects of computing and checking a result value are automated.

* They are composable: you can define a *X*let and use it as part of a bigger *X*let.

* They can express dependent sub-forms, i.e. the type and appearance of input fields in part of the form dynamically depends on user input in previous fields.

Using Forms is easy. Users might be frightened by their cryptic type signature so this guide intends to explain what is the meaning of all the elements that compose a Form. In addition, introductory examples will be presented to get the user acquainted with Forms.

We recommend to read [the Formlets documentation](http://websharper.com/docs/formlets) first, as some concepts, such as the `<*>` operator or validation, will be introduced with less detail here.

Forms is an implementation of piglets based on [WebSharper UI](http://github.com/intellifactory/ui). Therefore familiarity with concepts such as `Var`, `View` and the `Doc` type for HTML is necessary to work with it.


## A simple Form

Programming with reactive forms has two distinct steps:

* Defining your Form, i.e. defining the fields that compose the result, how they are composed, and what validation must be run on them.

* Rendering your Form, i.e. creating the markup that will be used and connecting the input fields with the reactive values created in the first step.

### Defining a Form

In this step we create a value of type `Form<'T, 'R>` where:

* `'T` is the type returned by the Form.

* `'R` is the type of the render builder. It will always have the following shape:

    ```
    (arg1 -> arg2 -> ... -> argn -> 'b) -> 'b
    ```

    which means that the view function from the second step will take arguments `arg1 ... argn` and return whatever type of markup element we want.

Let's create a reactive form to input data about a pet. We will need the species and the name of the pet. First, let's define the corresponding types:

```fsharp
type Species =
    | Cat | Dog | Piglet

    [<JavaScript>]
    override this.ToString() =
        match this with
        | Cat -> "cat"
        | Dog -> "dog"
        | Piglet -> "piglet"

type Pet = { species : Species; name : string }
```

Then, let's define the Form itself:

```fsharp
let PetForm (init: Pet) =
    Form.Return (fun s n -> { species = s; name = n })
    <*> Form.Yield init.species
    <*> (Form.Yield init.name
        |> Validation.IsNotEmpty "Please enter the pet's name.")
```

If you learned about Formlets already, this should look familiar. We first define a Form with a function type, and then successively compose it with each field. The main difference is that here, the fields do not declare how they will be rendered. `Form.Yield init.name` only creates a Form whose value has type `string` and which is initialized with `init.name`. Contrast with Formlet's `Controls.Input init.name`, which also declares that it should be rendered as an input field.

The types of `Form.Return`, `<*>` and `Form.Yield` are more complex than their Formlet counterparts, since they also deal with composing the view builder.

```fsharp
val Return : 'T -> Form<'T, ('D -> 'D)>

val Yield : 'T -> Form<'T, (Var<'T> -> 'D) -> 'D>

val (<*>) : Form<('T -> 'U), ('R -> 'R1)> ->
            Form<'T, ('R1 -> 'D)> ->
            Form<'U, ('R -> 'D)>
```

Validation is also very similar to Formlet validation: the Form is passed through a combinator, `Validation.IsNotEmpty`, that defines the condition that must be fulfilled and the error message in case it isn't.

We have now defined how a species and a name should be composed into a Pet, and how the name should be verified. Time to define how to render the corresponding reactive form.

### Rendering a Form

The Form we defined has the following type:

```fsharp
val PetForm : Pet ->
                Form<Pet,
                       (Var<Species> ->
                        Var<string> ->
                        'b) -> 'b>
```

The first type argument to our form is `Pet`, as expected, since that's what we want to return. The second type argument has the shape described previously: it takes as argument a function from several arguments (two `Var`s), and calls it with the appropriate `Var`s to obtain the rendered document.

```fsharp
let RenderPet species name =
    div [
        label [Doc.Radio [] Cat species; text (string Cat)]
        label [Doc.Radio [] Dog species; text (string Dog)]
        label [Doc.Radio [] Piglet species; text (string Piglet)]
        Doc.Input [] name
    ]
```

Here, `species` has type `Var<Species>`, and `name` has type `Var<string>`. So the type of `RenderPet` corresponds to the argument of the second type parameter of `PetForm`, with `'b` specialized to `Element`.

The functions `Doc.Radio` and `Doc.Input` come from WebSharper.UI, and create elements whose value is always synchronized with the `Var` they receive. Note that, unlike Formlets which include layout markup, these functions only render the needed input elements, allowing you to lay them out and style them as you want. For example, you can add attributes directly to the input element:

```fsharp
Doc.Input [attr.class "pet-name"] name
```

In order to use `RenderPet` to render the pet form, we use `Form.Render`:

```fsharp
let PetFormUI =
    PetForm { species = Cat; name = "Fluffy" }
    |> Form.Render RenderPet
```

We now have a value `PetFormUI : Doc` that we can integrate directly into our HTML markup. It will display a radio list and a text input field, and update the resulting `Pet` value according to user input in these two fields.

Note that right now, we are not doing anything with this resulting `Pet`. The simplest way to do so is using `Form.Run`, which calls a function every time the value is changed.

```fsharp
let PetFormUI =
    PetForm { species = Cat; name = "Fluffy" }
    |> Form.Run (fun animal ->
        JavaScript.Alert (
            "Your " + string animal.species +
            " is called " + animal.name))
    |> Form.Render RenderPet
```

## More complex Forms

### Submit button

The above `PetFormUI` is not very user friendly: it triggers (and shows an alert window) every time the user inputs a character. Let's fix this by adding a submit button to the underlying form.

```fsharp
let PetFormWithSubmit (init: Pet) =
    Form.Return (fun s n -> { species = s; name = n })
    <*> Form.Yield init.species
    <*> (Form.Yield init.name
        |> Validation.IsNotEmpty "Please enter the pet's name.")
    |> Form.WithSubmit
```

Now `PetFormWithSubmit` only triggers a new return value when the user submits the form. A new value of type `Submitter<Pet>` is passed to the view function, and rendering it is just as simple:

```fsharp
let RenderPet species name submitter =
    div [
        label [Doc.Radio [] Cat species; text (string Cat)]
        label [Doc.Radio [] Dog species; text (string Dog)]
        label [Doc.Radio [] Piglet species; text (string Piglet)]
        Doc.Input [] name
        Doc.Button [] submitter.Trigger
    ]
```

If you want the submit button to be grayed out when the input is invalid (i.e. in our case, when the name field is empty), use `Doc.ButtonValidate` instead.

### Displaying values and error messages

We have already seen `Form.Run`; but another common action to do with the result value is to display it. You can get the result from the `View` property on the submitter. It has a value of the following type:

```fsharp
type Result<'T> =
    | Success of 'T
    | Failure of ErrorMessage list
```

where `ErrorMessage` has a `Text` field containing the text message. Here is an example:

```fsharp
let RenderPetWithSubmit species name submit =
    div [
        submit.View
        |> View.Map (function
            | Success pet ->
                Doc.Concat [
                    span [text ("Your " + string pet.species + " is called ")]
                    b [text pet.name]
                ]
            | Failure errors ->
                Doc.Concat [
                    for error in errors do
                        yield bAttr [attr.style "color:red"] [text error.Text] :> _
                ])
        |> Doc.EmbedView
    ]
```

Note that we've been showing the result after submission. If you want to use the live value as it is input by the user, either to display it or for some other purpose, it is available as `submit.Input`.

### Forms for collections

Let's make this form more complex by asking the user about their own name and a list of their pets. They will be able to add, remove and reorder pets in the form.

Here is the final data we want to collect:

```fsharp
type Person =
    {
        firstName: string
        lastName: string
        pets: seq<Pet>
    }
```

Defining a Form for this type is relatively straightforward using a function from the `Form.Many*` family:

```fsharp
let PersonForm (init: Person) =
    Form.Return (fun first last pets ->
        { firstName = first; lastName = last; pets = pets })
    <*> (Form.Yield init.firstName
        |> Validation.Is Validation.NotEmpty "Please enter your first name.")
    <*> (Form.Yield init.lastName
        |> Validation.Is Validation.NotEmpty "Please enter your last name.")
    <*> Form.Many init.pets { species = Cat; name = "" } PetForm
    |> Form.WithSubmit
```

The function `Form.Many` takes three arguments:

* The initial collection of values, of type `seq<Pet>`.

* The value of type `Pet` with which the new sub-form should be initialized when the user inserts a new pet.

* A function taking an initial `Pet` value and returning the `Form<Pet, _>` that will be shown for each pet.

It returns a Form whose value is a sequence of `Pet`s, and adds an argument to the render function of type `Form.Many.CollectionWithDefault<'T, 'V, 'W>`. The type `'T` is the type of items in the collection, and `'V -> 'W` is the type of the render builder for a single item. This is how you render such a stream:

```fsharp
let RenderPerson (firstName: Var<string>)
                 (lastName: Var<string>)
                 (pets: Form.Many.CollectionWithDefault<Pet,_,_>)
                 (submit: Submitter<Result<_>>) =
    div [
        div [Doc.Input [] firstName]
        div [Doc.Input [] lastName]
        pets.Render (fun ops species name ->
            div [
                RenderPet species name
                Doc.ButtonValidate "Move up" [] ops.MoveUp
                Doc.ButtonValidate "Move down" [] ops.MoveDown
                Doc.Button "Delete" [] ops.Delete
            ])
        Doc.Button "Add a pet" [] pets.Add
        Doc.ButtonValidate "Submit" [] submit
    ]
```

The function passed to `pets.Render` is called once for every new item in the collection, and defines how this individual item should be rendered. It takes as arguments:

* A value of type `Form.Many.ItemOperations`, named `ops` here. This value has members that allow to move the current item up or down in the collection, or to delete it.

* The arguments of the render function for the item rendering Form, ie. the Form that was passed as the third argument to `Form.Many`.

`CollectionWithDefault` also contains a callback called `Add` that adds a new pet at the end of the collection.

### Localized errors

We have seen how to show all the errors together from `submit`. But in many cases it is useful to show the error associated with a given field next to that field. For that purpose, the type `View<Result<'T>>` has an extension method `Through` that takes a `Var` or a `Form`, and returns a new `View<Result<'T>>` whose value is the same as the original one, except on failure, only error messages associated with the given `Var` or `Form` are kept. For example, the following shows the error messages associated with `firstName`:

```fsharp
submit.View.Through firstName
|> View.Map (function
    | Success _ -> Doc.Empty
    | Failure errors ->
        Doc.Concat [
            for error in errors do
                yield bAttr [attr.style "color:red"] [text error.Text] :> _
        ]
)
|> Doc.EmbedView
```

## Complete example

Here is now the complete example, showcasing all the elements described in this tutorial.

```fsharp
type Species =
    | Cat | Dog | Piglet
    [<JavaScript>]
    override this.ToString() =
        match this with
        | Cat -> "cat"
        | Dog -> "dog"
        | Piglet -> "piglet"

type Pet = { species: Species; name: string }
type Person = { firstName: string; lastName: string; pets: seq<Pet> }

let PetForm (init: Pet) =
    Form.Return (fun s n -> { species = s; name = n })
    <*> Form.Yield init.species
    <*> (Form.Yield init.name
        |> Validation.IsNotEmpty "Please enter your pet's name.")

let PersonForm (init: Person) =
    Form.Return (fun first last pets ->
        { firstName = first; lastName = last; pets = pets })
    <*> (Form.Yield init.firstName
        |> Validation.IsNotEmpty "Please enter your first name.")
    <*> (Form.Yield init.lastName
        |> Validation.IsNotEmpty "Please enter your last name.")
    <*> Form.Many init.pets { species = Cat; name = "" } PetForm
    |> Form.WithSubmit

let RenderPet species name =
    Doc.Concat [
        label [Doc.Radio [] Cat species; text (string Cat)]
        label [Doc.Radio [] Dog species; text (string Dog)]
        label [Doc.Radio [] Piglet species; text (string Piglet)]
        Doc.Input [] name
    ]

let ShowErrorsFor v =
    v
    |> View.Map (function
        | Success _ -> Doc.Empty
        | Failure errors ->
            Doc.Concat [
                for error in errors do
                    yield bAttr [attr.style "color:red"] [text error.Text] :> _
            ]
    )
    |> Doc.EmbedView

let RenderPerson (firstName: Var<string>)
                 (lastName: Var<string>)
                 (pets: Form.Many.CollectionWithDefault<Pet,_,_>)
                 (submit: Submitter<Result<_>>) =
    div [
        h2 [text "You"]
        div [
            label [text "First name: "; Doc.Input [] firstName]
            ShowErrorsFor (submit.View.Through firstName)
        ]
        div [
            label [text "Last name: "; Doc.Input [] lastName]
            ShowErrorsFor (submit.View.Through lastName)
        ]
        h2 [text "Your pets"]
        div [
            pets.Render (fun ops species name ->
                div [
                    RenderPet species name
                    Doc.ButtonValidate "Move up" [] ops.MoveUp
                    Doc.ButtonValidate "Move down" [] ops.MoveDown
                    Doc.Button "Delete" [] ops.Delete
                    ShowErrorsFor (submit.View.Through name)
                ])
            Doc.Button "Add a pet" [] pets.Add
        ]
        div [
            Doc.Button "Submit" [] submit.Trigger
        ]
    ]

let Form =
    PersonForm {
        firstName = ""
        lastName = ""
        pets = [||] }
    |> Form.Run (fun p ->
        let message =
            "Welcome to you " + p.firstName + " " + p.lastName +
            (p.pets
                |> Seq.map (fun pet ->
                    ", your " + string pet.species + " " + pet.name)
                |> String.concat "") +
            "!"
        JS.Alert message)
    |> Form.Render RenderPerson
```
