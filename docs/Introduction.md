# Introduction to Piglets

The WebSharper Piglets library provides a high-level abstraction for
working with web forms and constructing interactive user interfaces.
It is closely related to the [Formlets
library](http://websharper.com/docs/formlets) as they both provide the
capability to declaratively describe user data input such as forms,
including data validation and feedback. The main difference comes from
the way actual widgets are rendered: Formlets automatically generate
input fields and layout markup, while Piglets let the developer render
the composed form using custom markup.

You should use Piglets in one of these cases:

* You want to have absolute control over the rendering of the form.

* You are creating an application for different frontends (e.g. a web
  version using JQueryUI and a mobile version using JQueryMobile) and
  would like to factor the data definition and validation code,
  leaving only the actual rendering to be differenciated.

You should use Formlets in one of these cases:

* You want your code to be extremely concise and define at the same
  time how input data is composed and validated, and how input fields
  are rendered.

* You are developing a back-office application, prioritizing speed of
  development over pixel-perfect design.

* You require dependent sub-forms, i.e. the type of input fields in
  part of the form dynamically depends on user input in previous
  fields.

In all cases, Piglets and Formlets have a lot in common:

* They are type-safe: unlike many "stringly typed" templating engines,
  in which a field is only identified by an id and there is no way to
  check that, say, a datepicker is indeed associated with a field of
  type `Date`, Formlets and Piglets are able to guarantee such
  properties.

* Data composition and validation is done declaratively, and the
  dynamic aspects of computing and checking a result value are
  automated.

* They are composable: you can define a *X*let and use it as part of a
  bigger *X*let.

Using Piglets is easy. Users might be frightened by their cryptic type
signature so this guide intends to explain what is the meaning of all
the elements that compose a Piglet. In addition, introductory examples
will be presented to get the user acquainted with Piglets.

We recommend to read [the Formlets
documentation](http://websharper.com/docs/formlets) first, as some
concepts, such as the `<*>` operator or validation, will be introduced
with less detail here.


## A simple Piglet

Using piglets is composed of two distinct steps:

* Defining the piglet, i.e. defining the fields that compose the
  result, how they are composed, and what validation must be run on
  them.

* Rendering the piglet, i.e. creating the markup that will be used and
  connecting the input fields with the reactive values created in the
  first step.

### Defining a Piglet

In this step we create a value of type `Piglet<'a, 'v>` where:

* `'a` is the type returned by the Piglet.

* `'v` is the type of the view builder. It will always have the
  following shape:

    ```
    (arg1 -> arg2 -> ... -> argn -> 'b) -> 'b
    ```

    which means that the view function from the second step will
    take arguments `arg1 ... argn` and return whatever type of markup
    element we want.

Let's create a piglet to input information about a pet. We will need
the species and the name of the pet. First, let's define the
corresponding types:

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

Then, let's define the Piglet itself:

```fsharp
let PetPiglet (init: Pet) =
    Piglet.Return (fun s n -> { species = s; name = n })
    <*> Piglet.Yield init.species
    <*> (Piglet.Yield init.name
        |> Validation.Is Validation.NotEmpty "Please enter the pet's name.")
```

If you learned about Formlets already, this should look familiar. We
first define a Piglet with a function type, and then successively
compose it with each field. The main difference is that here, the
fields do not declare how they will be rendered. `Piglet.Yield
init.name` only creates a Piglet whose value has type `string` and
which is initialized with `init.name`. Contrast with Formlet's
`Controls.Input init.name`, which also declares that it should be
rendered as an input field.

The types of `Piglet.Return`, `<*>` and `Piglet.Yield` are more
complex than their Formlet counterparts, since they also deal with
composing the view builder.

```fsharp
val Return : 'a -> Piglet<'a, ('b -> 'b)>

val Yield : 'a -> Piglet<'a, (Stream<'a> -> 'b) -> 'b>

val (<*>) : Piglet<('a -> 'b), ('c -> 'd)> ->
            Piglet<'a, ('d -> 'e)> ->
            Piglet<'b, ('c -> 'e)>
```

Validation is also very similar to Formlet validation: the Piglet is
passed through a combinator, `Validation.Is`, that defines the
condition that must be fulfilled and the error messaged in case it
isn't.

We have now defined how a species and a name should be composed into a
Pet, and how the name should be verified. Time to define how to render
the corresponding form.

### Rendering a Piglet

The Piglet we defined has the following type:

```fsharp
val PetPiglet : Pet ->
                Piglet<Pet,
                       (Stream<Species> ->
                        Stream<string> ->
                        'b) -> 'b>
```

The first type argument is `Pet`, as expected since that's what we
want to return. The second type argument has the shape described
previously; but what is this `Stream<_>` type? To put it simply, it
represents a value that can change over time. It is what we will use
to communicate with the input fields. Rendering is defined as follows:

```fsharp
let RenderPet species name =
    Div [
        Controls.Radio species [
            Cat, string Cat
            Dog, string Dog
            Piglet, string Piglet
        ]
        Controls.Input name
    ]
```

Here, `species` has type `Stream<Species>`, and `name` has type
`Stream<string>`. So the type of `RenderPet` corresponds to the
argument of the second type parameter of `PetPiglet`, with `'b`
specialized to `Element`.

The functions `Controls.Radio` and `Controls.Input` create elements
whose value is always synchronized with the `Stream` they receive.
Note that, unlike Formlets which include layout markup, these
functions only render the needed input elements, allowing you to style
them as you want. You can even add attributes directly to the input
element:

```fsharp
Controls.Input name -< [Attr.Class "pet-name"]
```

In order to use `RenderPet` to render the previously defined piglet,
we use the function `Piglet.Render`:

```fsharp
let PetForm =
    PetPiglet { species = Cat; name = "Fluffy" }
    |> Piglet.Render RenderPet
```

We now have a value `PetForm : Element` that we can integrate directly
into our HTML markup. It will display a radio list and a text input
field, and update the resulting `Pet` value according to user input in
these two fields.

Note that right now, we are not doing anything with this resulting
`Pet`. The simplest way to do so is using `Piglet.Run`, which calls a
function every time the value is changed.

```fsharp
let PetForm =
    PetPiglet { species = Cat; name = "Fluffy" }
    |> Piglet.Run (fun animal ->
        JavaScript.Alert (
            "Your " + string animal.species +
            " is called " + animal.name))
    |> Piglet.Render RenderPet
```

## More complex Piglets

### Submit button

The above Piglet is not very user friendly: it triggers (and shows an
alert window) every time the user inputs a character. Let's fix this
by adding a submit button.

```fsharp
let PetPigletWithSubmit (init: Pet) =
    Piglet.Return (fun s n -> { species = s; name = n })
    <*> Piglet.Yield init.species
    <*> (Piglet.Yield init.name
        |> Validation.Is Validation.NotEmpty "Please enter the pet's name.")
    |> Piglet.WithSubmit
```

Now `PetPiglet` only triggers a new return value when the user submits
the form. A new value of type `Submitter<Pet>` is passed to the view
function, and rendering it is just as simple:

```fsharp
let RenderPetWithSubmit species name submit =
    Div [
        Controls.Radio species [
            Cat, string Cat
            Dog, string Dog
            Piglet, string Piglet
        ]
        Controls.Input name
        Controls.Submit submit
    ]
```

If you want the submit button to be grayed out when the input is
invalid (i.e. in our case, when the name field is empty), use
`Controls.SubmitValidate` instead.

### Displaying values and error messages

We have already seen `Piglet.Run`; but another common action to do
with the result value is to display it. You can do it by passing a
container element to `Controls.Show`:

```fsharp
let RenderPetWithSubmit species name submit =
    Div [
        // ...
        Div [] |> Controls.Show submit (fun pet ->
            [
                Span [Text ("Your " + string pet.species + " is called ")]
                B [Text pet.name]
            ])
    ]
```

Similarly, to display the error messages, you can use
`Controls.ShowErrors`.

```fsharp
let RenderPetWithSubmit species name submit =
    Div [
        // ...
        Div [] |> Controls.ShowErrors submit (fun errors ->
            errors |> List.map (fun message ->
                B [Attr.Style "color:red"] -< [Text message]))
    ]
```

You can even combine the two using `Controls.ShowResult`. It passes
a value of the following type:

```fsharp
type Result<'a> =
    | Success of 'a
    | Failure of ErrorMessage list
```

where `ErrorMessage` has a `Message` field containing the text
message. Here is an example use of `ShowResult`:

```fsharp
let RenderPetWithSubmit species name submit =
    Div [
        // ...
        Div [] |> Controls.ShowResult submit (function
            | Success pet ->
                [
                    Span [Text ("Your " + string pet.species + " is called ")]
                    B [Text pet.name]
                ]
            | Failure errors ->
                errors |> List.map (fun error ->
                    B [Attr.Style "color:red"] -< [Text error.Message]))
    ]
```

Note that we've been showing the result after submission. If you want
to use the live value as it is input by the user, either to pass it to
`Controls.Show*` or for some other purpose, it is available as
`submit.Input`.

### Piglet collections

Let's make this form more complex by asking the user about their own
name and a list of their pets. They will be able to add, remove and
reorder pets in the form.

Here is the final data we want to collect:

```fsharp
type Person =
    {
        firstName: string
        lastName: string
        pets: Pet[]
    }
```

Defining a Piglet for this type is relatively straightforward using
a function from the `Piglet.Many*` family:

```fsharp
let PersonPiglet (init: Person) =
    Piglet.Return (fun first last pets ->
        { firstName = first; lastName = last; pets = pets })
    <*> (Piglet.Yield init.firstName
        |> Validation.Is Validation.NotEmpty "Please enter your first name.")
    <*> (Piglet.Yield init.lastName
        |> Validation.Is Validation.NotEmpty "Please enter your last name.")
    <*> Piglet.ManyInit init.pets
            { species = Cat; name = "" }
            PetPiglet
    |> Piglet.WithSubmit
```

The function `Piglet.ManyInit` takes three arguments:

* The initial array of values.

* The value with which the new sub-form should be initialized when the
  user inserts a new pet.

* A function taking an initial value and returning the Piglet that
  will be shown for each pet.

It returns a Piglet whose value is an array of `Pet`s, and adds an
argument to the render function of type `Many.UnitStream<'a, 'v, 'w>`.
The exact meaning of these type arguments is not important to
understand. This is how you render such a stream:

```fsharp
let RenderPerson firstName lastName pets submit =
    Div [
        Controls.Input firstName
        Controls.Input lastName
        Div [] |> Controls.RenderMany pets (fun ops species name ->
            Div [
                RenderPet species name
                Controls.Button ops.MoveUp -< [Text "Move up"]
                Controls.Button ops.MoveDown -< [Text "Move down"]
                Controls.Button ops.Delete -< [Text "Delete"]
            ])
        Controls.Button pets.Add -< [Text "Add a pet"]
        Controls.Submit submit
    ]
```

The function `Controls.RenderMany` takes three arguments:

* The `Many.UnitStream` to be rendered.

* A function that describes how to render each individual element of
  the collection. It is identical to the sub-Piglet's render function,
  except it takes an extra initial argument (which here we called
  `ops`) of type `Many.Operations`. This `ops` provides streams to
  manipulate the position of the current element in the array: move it
  up, down, or delete it entirely.

* A container, in our case it is the `Div []` that gets pipelined into
  `Controls.RenderMany`.

The `Many.UnitStream` also contains a stream called `Add` that allows us
to add a new pet at the end of the array.

### Localized errors

We have seen how to show together all the errors from `submit`. But in
many cases it is useful to show the error associated with a given
field next to that field. Unfortunately, the following doesn't produce
the desired effect:

```fsharp
Controls.ShowErrors firstName (fun messages -> (* ... *))
```

That's because the validator doesn't make the input stream itself
fail. Indeed, an invalid stream doesn't have a current value, so what
would we show in the input text box? Instead, the error is directly
propagated to the higher-level piglet. But the error knows which
individual stream has an invalid value, so we can use this to filter
error messages.

```fsharp
Controls.ShowErrors (firstName.Through submit) (fun messages -> (* ... *))
```

## Complete example

Here is now the complete example, showcasing all the elements described
in this tutorial.

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
type Person = { firstName: string; lastName: string; pets: Pet[] }

let PetPiglet (init: Pet) =
    Piglet.Return (fun s n -> { species = s; name = n })
    <*> Piglet.Yield init.species
    <*> (Piglet.Yield init.name
        |> Validation.Is Validation.NotEmpty "Please enter the pet's name.")

let PersonPiglet (init: Person) =
    Piglet.Return (fun first last pets ->
        { firstName = first; lastName = last; pets = pets })
    <*> (Piglet.Yield init.firstName
        |> Validation.Is Validation.NotEmpty "Please enter your first name.")
    <*> (Piglet.Yield init.lastName
        |> Validation.Is Validation.NotEmpty "Please enter your last name.")
    <*> Piglet.ManyInit init.pets
            { species = Cat; name = "" }
            PetPiglet
    |> Piglet.WithSubmit

let RenderPet species name =
    Div [
        Controls.Radio species [
            Cat, string Cat
            Dog, string Dog
            Piglet, string Piglet
        ]
        Controls.Input name
    ]

let RenderPerson firstName lastName pets submit =
    Div [
        Div [
            Controls.Input firstName
            Span [Attr.Class "color:red"]
                |> Controls.ShowErrors (firstName.Through submit)
                    (fun errors -> List.map Text errors)
        ]
        Div [
            Controls.Input lastName
            Span [Attr.Class "color:red"]
                |> Controls.ShowErrors (lastName.Through submit)
                    (fun errors -> List.map Text errors)
        ]
        Div [] |> Controls.RenderMany pets (fun ops species name ->
            Div [
                RenderPet species name
                Controls.Button ops.MoveUp -< [Text "Move up"]
                Controls.Button ops.MoveDown -< [Text "Move down"]
                Controls.Button ops.Delete -< [Text "Delete"]
            ])
        Controls.Button pets.Add -< [Text "Add a pet"]
        Controls.Submit submit
    ]

let Form =
    PersonPiglet {
        firstName = ""
        lastName = ""
        pets = [||] }
    |> Piglet.Run (fun p ->
        let message =
            "Welcome to you " + p.firstName + " " + p.lastName +
            (p.pets
                |> Array.map (fun pet ->
                    ", your " + string pet.species + " " + pet.name)
                |> String.concat "") +
            "!"
        JavaScript.Alert message)
    |> Piglet.Render RenderPerson
```
