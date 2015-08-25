namespace WebSharper.UI.Next.Piglets

open WebSharper.UI.Next

type Result<'T> =
    | Success of 'T
    | Failure of list<string>

module Result =

    val Map :
        f: ('T -> 'U) ->
        r: Result<'T> ->
        Result<'U>

    val Apply :
        rf: Result<'T -> 'U> ->
        rx: Result<'T> ->
        Result<'U>

type Piglet<'T, 'R>

module Many =

    [<Class>]
    type Operations =
        member Delete : unit -> unit
        member MoveUp : Submitter<Result<bool>>
        member MoveDown : Submitter<Result<bool>>

    [<Class>]
    type Stream<'T, 'V, 'W, 'Y, 'Z when 'W :> Doc and 'Z :> Doc> =

        member View : View<Result<seq<'T>>>

        ///Render the element collection inside this Piglet inside the given container and with the provided rendering function
        member Render : (Operations -> 'V) -> Doc

        ///Stream where new elements for the collection are written
        member Add : 'T -> unit

        ///Function that provides the Adder Piglet with a rendering function
        member RenderAdder : 'Y -> Doc

    [<Class>]
    type UnitStream<'T, 'V, 'W when 'W :> Doc> =
        inherit Stream<'T,'V,'W,'V,'W>

        ///Add an element to the collection set to the default values
        member Add : unit -> unit

[<Sealed>]
type Chooser<'Out, 'In, 'U, 'V, 'W, 'X when 'In : equality and 'X :> Doc> =
        
    member View : View<Result<'Out>>

    member Chooser : 'U -> 'V

    member Choice : 'W -> Doc

module Piglet =

    val Create
         : view: View<Result<'T>>
        -> renderBuilder: ('R -> 'D)
        -> Piglet<'T, 'R -> 'D>

    val Render
         : renderFunction: 'R
        -> Piglet<'T, 'R -> #Doc>
        -> Doc

    val GetView
         : Piglet<'T, 'R -> 'D>
        -> View<Result<'T>>

    val Return
         : value: 'T
        -> Piglet<'T, 'D -> 'D>

    val ReturnFailure
         : unit
        -> Piglet<'T, 'D -> 'D>

    val Yield
         : value: 'T
        -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

    val YieldFailure
         : unit
        -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

    val YieldOption
         : init: option<'T>
        -> noneValue: 'T
        -> Piglet<option<'T>, (Var<'T> -> 'D) -> 'D>
        when 'T : equality

    val Apply
         : Piglet<'T -> 'U, 'R -> 'R1>
        -> Piglet<'T, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val WithSubmit
         : Piglet<'T, 'R -> Submitter<Result<'T>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val TransmitView
         : Piglet<'T, 'R -> View<Result<'T>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val TransmitViewMap
         : ('T -> 'U)
        -> Piglet<'T, 'R -> View<Result<'U>> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val TransmitViewMapResult
         : (Result<'T> -> 'U)
        -> Piglet<'T, 'R -> View<'U> -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val Map
         : ('T -> 'U)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapToResult
         : ('T -> Result<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapResult
         : (Result<'T> -> Result<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapAsync
         : ('T -> Async<'U>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapToAsyncResult
         : ('T -> Async<Result<'U>>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapAsyncResult
         : (Result<'T> -> Async<Result<'U>>)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val MapViewArgs
         : 'R1
         -> Piglet<'T, 'R1 -> 'R2>
         -> Piglet<'T, ('R2 -> 'D) -> 'D>

    val FlushErrors
         : Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val Run
         : ('T -> unit)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

    val RunResult
         : (Result<'T> -> unit)
        -> Piglet<'T, 'R -> 'D>
        -> Piglet<'T, 'R -> 'D>

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
        -> Piglet<seq<'T>, (Many.Stream<'T, 'V, 'W, 'Y, 'Z> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    /// Create a Piglet that returns a collection of values, each created according to the given Piglet.
    val Many
         : init: seq<'T>
        -> addValue: 'T
        -> itemPiglet: ('T -> Piglet<'T, 'V -> 'W>)
        -> Piglet<seq<'T>, (Many.UnitStream<'T, 'V, 'W> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    type Builder =
        | Do

        member Bind : Piglet<'In, 'U -> 'V> * ('In -> Piglet<'Out, 'W -> 'X>) -> Piglet<'Out, (Chooser<'Out, 'In, 'U, 'V, 'W, 'X> -> 'Y) -> 'Y>

        member Return : 'T -> Piglet<'T, 'D -> 'D>

        member ReturnFrom : Piglet<'T, 'R -> 'D> -> Piglet<'T, 'R -> 'D>

        member Yield : 'T -> Piglet<'T, (Var<'T> -> 'D) -> 'D>

        member YieldFrom : Piglet<'T, 'R -> 'D> -> Piglet<'T, 'R -> 'D>

        member Zero : unit -> Piglet<'T, 'D -> 'D>

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

    val (<*>)
         : pf: Piglet<'T -> 'U, 'R -> 'R1>
        -> px: Piglet<'T, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

    val (<*?>)
         : pf: Piglet<'T -> 'U, 'R -> 'R1>
        -> px: Piglet<Result<'T>, 'R1 -> 'D>
        -> Piglet<'U, 'R -> 'D>

module Doc =

    val ButtonValidate
         : caption: string
        -> seq<Attr>
        -> Submitter<Result<'T>>
        -> Elt