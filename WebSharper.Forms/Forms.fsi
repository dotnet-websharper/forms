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
namespace WebSharper.Forms

open System.Runtime.CompilerServices
open WebSharper
open WebSharper.UI
open WebSharper.UI.Client

[<Sealed>]
type ErrorMessage =
    member Id: string
    member Text: string
    static member Create : id: string * text: string -> ErrorMessage
    static member Create : Form<'T, 'R> * text: string -> ErrorMessage

and Result<'T> =
    | Success of 'T
    | Failure of list<ErrorMessage>

and [<Sealed>] Form<'T, 'R> =
    member Id : string
    member View : View<Result<'T>>
    member Render : 'R

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
         * ?id: string
        -> Result<'T>


/// Form constructors and combinators.
module Form =

    /// Operations related to Forms of collections.
    module Many =

        /// Operations applicable to an item in a Form of collections.
        [<Class>]
        type ItemOperations =

            /// Delete the current item from the collection.
            member Delete : unit -> unit

            /// Move the current item up one step in the collection.
            member MoveUp : Submitter<Result<bool>>

            /// Move the current item down one step in the collection.
            member MoveDown : Submitter<Result<bool>>

        /// Operations applicable to a Form of collections.
        [<Class>]
        type Collection<'T, 'V, 'W, 'Y, 'Z when 'W :> Doc and 'Z :> Doc> =

            /// A view on the resulting collection.
            member View : View<Result<seq<'T>>>

            /// Render the item collection inside this Form
            /// with the provided rendering function.
            member Render : (ItemOperations -> 'V) -> Doc

            /// Stream where new items for the collection are written.
            member Add : 'T -> unit

            /// Render the Form that inserts new items into the collection.
            member RenderAdder : 'Y -> Doc

        /// Operations applicable to a Form of collections
        /// with a provided default new value to insert.
        [<Class>]
        type CollectionWithDefault<'T, 'V, 'W when 'W :> Doc> =
            inherit Collection<'T,'V,'W,'V,'W>

            /// Add an item to the collection set to the default value.
            [<Name "AddOne">]
            member Add : unit -> unit

    /// Operations applicable to a dependent Form.
    [<Sealed>]
    type Dependent<'TResult, 'U, 'W> =
        
        /// A view on the result of the dependent Form.
        member View : View<Result<'TResult>>

        /// Render the primary part of a dependent Form.
        member RenderPrimary : 'U -> Doc

        /// Render the dependent part of a dependent Form.
        member RenderDependent : 'W -> Doc

    /// Create a Form from a view and a render builder.
    val Create
         : view: View<Result<'T>>
        -> renderBuilder: ('R -> 'D)
        -> Form<'T, 'R -> 'D>

    /// Render a Form with a render function.
    val Render
         : renderFunction: 'R
        -> Form<'T, 'R -> #Doc>
        -> Doc

    /// Render the items of a Collection with the provided rendering function.
    val RenderMany
            : Many.Collection<'T, 'V, 'W, 'Y, 'Z>
        -> (Many.ItemOperations -> 'V)
        -> Doc
        when 'W :> Doc and 'Z :> Doc

    /// Render the Form that inserts new items into a Collection.
    val RenderManyAdder
            : Many.Collection<'T, 'V, 'W, 'Y, 'Z>
        -> 'Y
        -> Doc
        when 'W :> Doc and 'Z :> Doc

    /// Render the primary part of a dependent Form.
    val RenderPrimary
         : Dependent<'TResult, 'U, 'W>
        -> 'U
        -> Doc

    /// Render the dependent part of a dependent Form.
    val RenderDependent
         : Dependent<'TResult, 'U, 'W>
        -> 'W
        -> Doc

    /// Get the view of a Form.
    val GetView
         : Form<'T, 'R -> 'D>
        -> View<Result<'T>>

    /// Create a Form that always returns the same successful value.
    val Return
         : value: 'T
        -> Form<'T, 'D -> 'D>

    /// Create a Form that always fails.
    val ReturnFailure
         : unit
        -> Form<'T, 'D -> 'D>

    /// Create a Form that returns a reactive value,
    /// initialized to a successful value `init`.
    val Yield
         : init: 'T
        -> Form<'T, (Var<'T> -> 'D) -> 'D>

    /// Create a Form that returns a reactive value.
    val YieldVar
         : Var<'T>
        -> Form<'T, (Var<'T> -> 'D) -> 'D>

    /// Create a Form that returns a reactive value, initialized to failure.
    val YieldFailure
         : unit
        -> Form<'T, (Var<'T> -> 'D) -> 'D>

    /// Create a Form that returns a reactive optional value,
    /// initialized to a successful value `init`.
    ///
    /// When the associated Var is `noneValue`, the result value is `None`;
    /// when it is any other value `x`, the result value is `Some x`.
    val YieldOption
         : init: option<'T>
        -> noneValue: 'T
        -> Form<option<'T>, (Var<'T> -> 'D) -> 'D>
        when 'T : equality

    /// Apply a Form that returns a function to a Form that returns a value.
    val Apply
         : Form<'T -> 'U, 'R -> 'R1>
        -> Form<'T, 'R1 -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Add a submitter to a Form: the returned Form gets its value from
    /// the input Form whenever the submitter is triggered.
    val WithSubmit
         : Form<'T, 'R -> Submitter<Result<'T>> -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Pass a view on the result of a Form to its render function.
    val TransmitView
         : Form<'T, 'R -> View<Result<'T>> -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Pass a mapped view on the result of a Form to its render function.
    val TransmitViewMap
         : ('T -> 'U)
        -> Form<'T, 'R -> View<Result<'U>> -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Pass a mapped view on the result of a Form to its render function.
    val TransmitViewMapResult
         : (Result<'T> -> 'U)
        -> Form<'T, 'R -> View<'U> -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Map the result of a Form.
    val Map
         : ('T -> 'U)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the result of a Form.
    val MapToResult
         : ('T -> Result<'U>)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the result of a Form.
    val MapResult
         : (Result<'T> -> Result<'U>)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the result of a Form asynchronously.
    val MapAsync
         : ('T -> Async<'U>)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the result of a Form asynchronously.
    val MapToAsyncResult
         : ('T -> Async<Result<'U>>)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the result of a Form asynchronously.
    val MapAsyncResult
         : (Result<'T> -> Async<Result<'U>>)
        -> Form<'T, 'R -> 'D>
        -> Form<'U, 'R -> 'D>

    /// Map the arguments passed to the render function of a Form.
    val MapRenderArgs
         : 'R1
         -> Form<'T, 'R1 -> 'R2>
         -> Form<'T, ('R2 -> 'D) -> 'D>

    /// Map any failing result to a failure with no error messages.
    val FlushErrors
         : Form<'T, 'R -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Run a function on all successful results.
    val Run
         : ('T -> unit)
        -> Form<'T, 'R -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Run a function on all results.
    val RunResult
         : (Result<'T> -> unit)
        -> Form<'T, 'R -> 'D>
        -> Form<'T, 'R -> 'D>

    /// Create a dependent Form where a `dependent` Form depends on the value of a `primary` Form.
    val Dependent
         : primary: Form<'TPrimary, 'U -> 'V>
        -> dependent: ('TPrimary -> Form<'TResult, 'W -> 'X>)
        -> Form<'TResult, (Dependent<'TResult, 'U, 'W> -> 'Y) -> 'Y>
        when 'TPrimary : equality and 'V :> Doc and 'X :> Doc

    /// Create a Form that returns a collection of values,
    /// with an additional Form used to insert new values in the collection.
    val ManyForm
         : init: seq<'T>
        -> addForm: (Form<'T, 'Y -> 'Z>)
        -> itemForm: ('T -> Form<'T, 'V -> 'W>)
        -> Form<seq<'T>, (Many.Collection<'T, 'V, 'W, 'Y, 'Z> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    /// Create a Form that returns a collection of values, each created according to the given Form.
    val Many
         : init: seq<'T>
        -> addValue: 'T
        -> itemForm: ('T -> Form<'T, 'V -> 'W>)
        -> Form<seq<'T>, (Many.CollectionWithDefault<'T, 'V, 'W> -> 'x) -> 'x>
        when 'W :> Doc and 'Z :> Doc

    type Builder =
        | Do

        /// Create a dependent Form where the `dependent` part depends on an `primary` Form.
        member Bind
             : input: Form<'TPrimary, 'U -> 'V>
             * output: ('TPrimary -> Form<'TResult, 'W -> 'X>)
            -> Form<'TResult, (Dependent<'TResult, 'U, 'W> -> 'Y) -> 'Y>
            when 'TPrimary : equality and 'V :> Doc and 'X :> Doc

        /// Create a Form that always returns the same successful value.
        member Return : 'T -> Form<'T, 'D -> 'D>

        /// Return the given Form.
        member ReturnFrom : Form<'T, 'R -> 'D> -> Form<'T, 'R -> 'D>

        /// Create a Form that returns a reactive value,
        /// initialized to a successful value `init`.
        member Yield : init: 'T -> Form<'T, (Var<'T> -> 'D) -> 'D>

        /// Return the given Form.
        member YieldFrom : Form<'T, 'R -> 'D> -> Form<'T, 'R -> 'D>

        /// Create a Form that always fails.
        member Zero : unit -> Form<'T, 'D -> 'D>

/// Functions to validate the value of a Form.
module Validation =

    /// If the Form value passes the predicate, it is passed on;
    /// else, `Failwith msg` is passed on.
    val Is : pred: ('T -> bool) -> msg: string -> Form<'T, 'R -> 'D> -> Form<'T, 'R -> 'D>

    /// If the Form value is not an empty string, it is passed on;
    /// else, `Failwith msg` is passed on.
    val IsNotEmpty : msg: string -> Form<string, 'R -> 'D> -> Form<string, 'R -> 'D>

    /// If the Form value matches the given regexp, it is passed on;
    /// else, `Failwith msg` is passed on.
    val IsMatch : regexp: string -> msg: string -> Form<string, 'R -> 'D> -> Form<string, 'R -> 'D>

    val MapValidCheckedInput : msg: string -> Form<CheckedInput<'T>, 'R -> 'D> -> Form<'T, 'R -> 'D>

[<AutoOpen>]
module Pervasives =

    /// Apply a Form that returns a function to a Form that returns a value.
    val (<*>)
         : pf: Form<'T -> 'U, 'R -> 'R1>
        -> px: Form<'T, 'R1 -> 'D>
        -> Form<'U, 'R -> 'D>

    val (<*?>)
         : pf: Form<'T -> 'U, 'R -> 'R1>
        -> px: Form<Result<'T>, 'R1 -> 'D>
        -> Form<'U, 'R -> 'D>

module Attr =

    /// Add a click handler that triggers a Submitter,
    /// and disable the element when the submitter's input is a failure.
    val SubmitterValidate
         : Submitter<Result<'T>>
        -> Attr

module Doc =

    /// Create a button that triggers a Submitter when clicked,
    /// and is disabled when the submitter's input is a failure.
    val ButtonValidate
         : caption: string
        -> seq<Attr>
        -> Submitter<Result<'T>>
        -> Elt

    /// When the input View is a failure, show the given Doc;
    /// otherwise, show an empty Doc.
    val ShowErrors
         : View<Result<'T>>
        -> (list<ErrorMessage> -> Doc)
        -> Doc

    /// When the input View is a success, show the given Doc;
    /// otherwise, show an empty Doc.
    val ShowSuccess
         : View<Result<'T>>
        -> ('T -> Doc)
        -> Doc

module private Fresh =

    val Id : unit -> string

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
    /// to those that come directly from the given Form.
    [<Extension>]
    static member Through
         : input: View<Result<'T>>
         * Form<'U, 'R>
        -> View<Result<'T>>

    /// When the input View is a failure, show the given Doc;
    /// otherwise, show an empty Doc.
    [<Extension>]
    static member ShowErrors
         : View<Result<'T>>
         * (list<ErrorMessage> -> Doc)
        -> Doc

    /// When the input View is a success, show the given Doc;
    /// otherwise, show an empty Doc.
    [<Extension>]
    static member ShowSuccess
         : View<Result<'T>>
         * ('T -> Doc)
        -> Doc
