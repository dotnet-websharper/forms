namespace WebSharper.UI.Next.Piglets

open System.Runtime.CompilerServices
open WebSharper.UI.Next

[<Sealed>]
type ErrorMessage =
    member Text: string

type Result<'T> =
    | Success of 'T
    | Failure of list<ErrorMessage>

[<Sealed>]
type Result =

    /// Check whether a result is successful.
    static member IsSuccess
         : Result<'T>
        -> bool

    /// Check whether a result is failing.
    static member IsFailure
         : Result<'T>
        -> bool

    /// Pass a result through a function if it is successful.
    static member Map
         : f: ('T -> 'U)
        -> r: Result<'T>
        -> Result<'U>

    /// Apply a function result to a value result if both are successful.
    static member Apply
         : rf: Result<'T -> 'U>
        -> rx: Result<'T>
        -> Result<'U>

    /// Pass a result through a function if it is successful.
    static member Bind
         : f: ('T -> Result<'U>)
        -> r: Result<'T>
        -> Result<'U>

    /// Create a failing result with a single error message.
    static member FailWith
         : errorMessage: string
         * ?id: int
        -> Result<'T>

type Piglet<'T, 'R>

/// Operations related to Piglets of collections.
module Many =

    /// Operations applicable to an item in a Piglet of collections.
    [<Class>]
    type ItemOperations =

        /// Delete the current item from the collection.
        member Delete : unit -> unit

        /// Move the current item up one step in the collection.
        member MoveUp : Submitter<Result<bool>>

        /// Move the current item down one step in the collection.
        member MoveDown : Submitter<Result<bool>>

    /// Operations applicable to a Piglet of collections.
    [<Class>]
    type Collection<'T, 'V, 'W, 'Y, 'Z when 'W :> Doc and 'Z :> Doc> =

        /// A view on the resulting collection.
        member View : View<Result<seq<'T>>>

        /// Render the element collection inside this Piglet
        /// with the provided rendering function.
        member Render : (ItemOperations -> 'V) -> Doc

        /// Stream where new elements for the collection are written.
        member Add : 'T -> unit

        /// Render the Piglet that inserts new items into the collection.
        member RenderAdder : 'Y -> Doc

    /// Operations applicable to a Piglet of collections
    /// with a provided default new value to insert.
    [<Class>]
    type CollectionWithDefault<'T, 'V, 'W when 'W :> Doc> =
        inherit Collection<'T,'V,'W,'V,'W>

        /// Add an element to the collection set to the default value.
        member Add : unit -> unit

/// Operations applicable to a dependent Piglet.
[<Sealed>]
type Chooser<'Out, 'In, 'U, 'V, 'W, 'X when 'In : equality and 'X :> Doc> =
        
    /// A view on the result of the dependent Piglet.
    member View : View<Result<'Out>>

    /// Render the static part of a dependent Piglet.
    member Chooser : 'U -> 'V

    /// Render the dynamic part of a dependent Piglet.
    member Choice : 'W -> Doc

/// Piglet constructors and combinators.
module Piglet =

    /// Create a Piglet from a view and a render builder.
    val Create
         : view: View<Result<'T>>
        -> renderBuilder: ('R -> 'D)
        -> Piglet<'T, 'R -> 'D>

    /// Render a Piglet with a render function.
    val Render
         : renderFunction: 'R
        -> Piglet<'T, 'R -> #Doc>
        -> Doc

    /// Get the view of a Piglet.
    val GetView
         : Piglet<'T, 'R -> 'D>
        -> View<Result<'T>>

    /// Create a Piglet that always returns the same successful value.
    val Return
         : value: 'T
        -> Piglet<'T, 'D -> 'D>

    /// Create a Piglet that always fails.
    val ReturnFailure
         : unit
        -> Piglet<'T, 'D -> 'D>

    /// Create a Piglet that returns a reactive value,
    /// initialized to a successful value `init`.
    val Yield
         : init: 'T
        -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

    /// Create a Piglet that returns a reactive value, initialized to failure.
    val YieldFailure
         : unit
        -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

    /// Create a Piglet that returns a reactive optional value,
    /// initialized to a successful value `init`.
    ///
    /// When the associated Var is `noneValue`, the result value is `None`;
    /// when it is any other value `x`, the result value is `Some x`.
    val YieldOption
         : init: option<'T>
        -> noneValue: 'T
        -> Piglet<option<'T>, (Var<'T> -> 'D) -> 'D>
        when 'T : equality

    /// Apply a Piglet that returns a function to a Piglet that returns a value.
    val Apply
         : Piglet<'T -> 'U, 'R -> 'R1>
        -> Piglet<'T, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Add a submitter to a Piglet: the returned Piglet gets its value from
    /// the input Piglet whenever the submitter is triggered.
    val WithSubmit
         : Piglet<'T, 'R -> Submitter<Result<'T>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Pass a view on the result of a Piglet to its render function.
    val TransmitView
         : Piglet<'T, 'R -> View<Result<'T>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Pass a mapped view on the result of a Piglet to its render function.
    val TransmitViewMap
         : ('T -> 'U)
        -> Piglet<'T, 'R -> View<Result<'U>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Pass a mapped view on the result of a Piglet to its render function.
    val TransmitViewMapResult
         : (Result<'T> -> 'U)
        -> Piglet<'T, 'R -> View<'U> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Map the result of a Piglet.
    val Map
         : ('T -> 'U)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the result of a Piglet.
    val MapToResult
         : ('T -> Result<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the result of a Piglet.
    val MapResult
         : (Result<'T> -> Result<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the result of a Piglet asynchronously.
    val MapAsync
         : ('T -> Async<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the result of a Piglet asynchronously.
    val MapToAsyncResult
         : ('T -> Async<Result<'U>>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the result of a Piglet asynchronously.
    val MapAsyncResult
         : (Result<'T> -> Async<Result<'U>>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    /// Map the arguments passed to the render function of a Piglet.
    val MapRenderArgs
         : 'R1
         -> Piglet<'T, 'R1 -> 'R2>
         -> Piglet<'T, ('R2 -> 'D) -> 'D>

    /// Map any failing result to a failure with no error messages.
    val FlushErrors
         : Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Run a function on all successful results.
    val Run
         : ('T -> unit)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Run a function on all results.
    val RunResult
         : (Result<'T> -> unit)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

    /// Create a dynamic Piglet where the `output` part depends on an `input` Piglet.
    val Choose
         : input: Piglet<'In, 'U -> 'V>
        -> output: ('In -> Piglet<'Out, 'W -> 'X>)
        -> Piglet<'Out, (Chooser<'Out, 'In, 'U, 'V, 'W, 'X> -> 'Y) -> 'Y>
        when 'In : equality and 'X :> Doc

    /// Create a Piglet that returns a collection of values,
    /// with an additional piglet used to insert new values in the collection.
    val ManyPiglet
         : init: seq<'T>
        -> addPiglet: (Piglet<'T, 'Y -> 'Z>)
        -> itemPiglet: ('T -> Piglet<'T, 'V -> 'W>)
        -> Piglet<seq<'T>, (Many.Collection<'T, 'V, 'W, 'Y, 'Z> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    /// Create a Piglet that returns a collection of values, each created according to the given Piglet.
    val Many
         : init: seq<'T>
        -> addValue: 'T
        -> itemPiglet: ('T -> Piglet<'T, 'V -> 'W>)
        -> Piglet<seq<'T>, (Many.CollectionWithDefault<'T, 'V, 'W> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    type Builder =
        | Do

        /// Create a dynamic Piglet where the `output` part depends on an `input` Piglet.
        member Bind : input: Piglet<'In, 'U -> 'V> * output: ('In -> Piglet<'Out, 'W -> 'X>) -> Piglet<'Out, (Chooser<'Out, 'In, 'U, 'V, 'W, 'X> -> 'Y) -> 'Y>

        /// Create a Piglet that always returns the same successful value.
        member Return : 'T -> Piglet<'T, 'D -> 'D>

        /// Return the given Piglet.
        member ReturnFrom : Piglet<'T, 'R -> 'D> -> Piglet<'T, 'R -> 'D>

        /// Create a Piglet that returns a reactive value,
        /// initialized to a successful value `init`.
        member Yield : init: 'T -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

        /// Return the given Piglet.
        member YieldFrom : Piglet<'T, 'R -> 'D> -> Piglet<'T, 'R -> 'D>

        /// Create a Piglet that always fails.
        member Zero : unit -> Piglet<'T, 'D -> 'D>

/// Functions to validate the value of a Piglet.
module Validation =

    /// If the Piglet value passes the predicate, it is passed on;
    /// else, `Failwith msg` is passed on.
    val Is : pred: ('T -> bool) -> msg: string -> Piglet<'T, 'R -> 'D> -> Piglet<'T, 'R -> 'D>

    /// If the Piglet value is not an empty string, it is passed on;
    /// else, `Failwith msg` is passed on.
    val IsNotEmpty : msg: string -> Piglet<string, 'R -> 'D> -> Piglet<string, 'R -> 'D>

    /// If the Piglet value matches the given regexp, it is passed on;
    /// else, `Failwith msg` is passed on.
    val IsMatch : regexp: string -> msg: string -> Piglet<string, 'R -> 'D> -> Piglet<string, 'R -> 'D>

[<AutoOpen>]
module Pervasives =

    /// Apply a Piglet that returns a function to a Piglet that returns a value.
    val (<*>)
         : pf: Piglet<'T -> 'U, 'R -> 'R1>
        -> px: Piglet<'T, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val (<*?>)
         : pf: Piglet<'T -> 'U, 'R -> 'R1>
        -> px: Piglet<Result<'T>, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

module Doc =

    /// Create a button that triggers a Submitter when clicked,
    /// and is disabled when the submitter's input is a failure.
    val ButtonValidate
         : caption: string
        -> seq<Attr>
        -> Submitter<Result<'T>>
        -> Elt

module private Fresh =

    val Id : unit -> int

[<Extension; Sealed>]
type View =

    /// When the input View is a failure, restrict its error messages
    /// to those that come directly from the given Var.
    [<Extension>]
    static member Through
         : input: View<Result<'T>>
         * Var<'U>
        -> View<Result<'T>>

    /// When the input View is a failure, restrict its error messages
    /// to those that come directly from the given Piglet.
    [<Extension>]
    static member Through
         : input: View<Result<'T>>
         * Piglet<'U, 'R>
        -> View<Result<'T>>
