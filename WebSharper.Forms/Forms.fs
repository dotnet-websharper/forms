namespace WebSharper.Forms

open System.Runtime.CompilerServices
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Notation

[<JavaScript; Sealed>]
type ErrorMessage (id: string, message: string) =

    [<Inline>]
    member this.Id = id

    [<Inline>]
    member this.Text = message

[<JavaScript>]
module Fresh =

    let lastId = ref 0

    let Id() =
        incr lastId
        "Form" + string !lastId

[<JavaScript>]
type Result<'T> =
    | Success of 'T
    | Failure of list<ErrorMessage>
 
[<JavaScript; Sealed>]
type Result =

    static member IsSuccess (r: Result<'T>) =
        match r with
        | Success _ -> true
        | Failure _ -> false

    static member IsFailure (r: Result<'T>) =
        match r with
        | Success _ -> false
        | Failure _ -> true

    static member Map (f: 'T -> 'U) (r: Result<'T>) =
        match r with
        | Success x -> Success (f x)
        | Failure m -> Failure m

    static member Apply (rf: Result<'T -> 'U>) (rx: Result<'T>) =
        match rf with
        | Failure mf ->
            match rx with
            | Failure mx -> Failure (mf @ mx)
            | Success _ -> Failure mf
        | Success f ->
            match rx with
            | Failure mx -> Failure mx
            | Success x -> Success (f x)

    static member ApJoin (rf: Result<'T -> 'U>) (rx: Result<Result<'T>>) =
        match rf with
        | Failure mf ->
            match rx with
            | Failure mx -> Failure (mf @ mx)
            | Success _ -> Failure mf
        | Success f ->
            match rx with
            | Failure mx
            | Success (Failure mx) -> Failure mx
            | Success (Success x) -> Success (f x)

    static member Bind (f: 'T -> Result<'U>) (rx: Result<'T>) : Result<'U> =
        match rx with
        | Failure m -> Failure m
        | Success x -> f x

    static member Append (app: 'T -> 'T -> 'T) (r1: Result<'T>) (r2: Result<'T>) : Result<'T> =
        match r1 with
        | Failure m1 ->
            match r2 with
            | Failure m2 -> Failure (m1 @ m2)
            | Success _ -> r1
        | Success x1 ->
            match r2 with
            | Failure _ -> r2
            | Success x2 -> Success (app x1 x2)

    static member FailWith (errorMessage, ?id) =
        let id = match id with Some id -> id | None -> Fresh.Id()
        Failure [ErrorMessage(id, errorMessage)]

[<JavaScript>]
type Form<'T, 'R> =
    {
        id : string
        view : View<Result<'T>>
        render : 'R
    }

    member this.Id = this.id
    member this.View = this.view
    member this.Render = this.render

type ErrorMessage with

    [<JavaScript>]
    static member Create (id: string, text) =
        new ErrorMessage(id, text)

    [<JavaScript>]
    static member Create (p: Form<_, _>, text) =
        new ErrorMessage(p.id, text)

[<JavaScript; AutoOpen>]
module Utils =

    let memoize f =
        let d = System.Collections.Generic.Dictionary()
        fun x ->
            if d.ContainsKey x then
                d.[x]
            else
                let y = f x
                d.[x] <- y
                y


[<JavaScript>]
module Form =

    [<Sealed; JavaScript>]
    type Dependent<'TResult, 'U, 'W>
        (
            renderPrimary: 'U -> Doc,
            pOut: View<Result<Form<'TResult, 'W -> Doc>>>
        ) =

        let out =
            pOut.Bind (function
                | Success p -> p.view
                | Failure m -> View.Const (Failure m))
        
        member this.View : View<Result<'TResult>> = out

        member this.RenderPrimary (f: 'U) : Doc =
            renderPrimary f

        member this.RenderDependent (f: 'W) : Doc =
            pOut |> Doc.BindView (function
                | Success p -> p.render f
                | Failure _ -> Doc.Empty)

    module Dependent =
        let Make primary dependent =
            let dependent = memoize (fun x ->
                let p = dependent x
                { view = p.view; id = p.id; render = fun x -> p.render x :> Doc })
            let pOut = primary.view.Map (Result.Map dependent)
            Dependent((fun x -> primary.render x :> Doc), pOut)


    [<JavaScript>]
    module Many =

        type ItemOperations(delete: unit -> unit, moveUp: Submitter<Result<bool>>, moveDown: Submitter<Result<bool>>) =
            member this.Delete() = delete()
            member this.MoveUp = moveUp
            member this.MoveDown = moveDown

        type System.Collections.Generic.List<'T> with
            member this.Swap(i, j) =
                let tmp = this.[i]
                this.[i] <- this.[j]
                this.[j] <- tmp

        module Fresh =

            let Int =
                let x = ref 0
                fun () ->
                    incr x
                    !x

        type Collection<'T, 'V, 'W, 'Y, 'Z when 'W :> Doc and 'Z :> Doc> (p : 'T -> Form<'T, 'V -> 'W>, inits: seq<'T>, adder : Form<'T, 'Y -> 'Z>) =
            let arr = ResizeArray()
            let var = Var.Create arr
            let mk (x: 'T) =
                let ident = Fresh.Int()
                let getThisIndexIn = Seq.findIndex (fun (_, _, j) -> ident = j)
                let vIndex = var.View |> View.Map getThisIndexIn
                let delete() =
                    let k = getThisIndexIn arr
                    arr.RemoveAt k
                    Var.Update var id
                let sMoveUp =
                    let inp = vIndex |> View.Map (fun i ->
                        if i = 0 then Failure [] else Success true
                    )
                    Submitter.Create inp (if arr.Count = 0 then Failure [] else Success false)
                let vMoveUp =
                    sMoveUp.View |> View.Map (function
                        | Success true ->
                            let i = getThisIndexIn arr
                            arr.Swap(i, i - 1)
                            Var.Update var id
                        | _ -> ()
                    )
                let sMoveDown =
                    let inp = vIndex |> View.Map (fun i ->
                        if i = arr.Count - 1 then Failure [] else Success true
                    )
                    Submitter.Create inp (Failure [])
                let vMoveDown =
                    sMoveDown.View |> View.Map (function
                        | Success true ->
                            let i = getThisIndexIn arr
                            arr.Swap(i, i + 1)
                            Var.Update var id
                        | _ -> ()
                    )
                let p = p x
                let v = View.Map2 (fun x () -> x) p.view (View.Map2 (fun () () -> ()) vMoveUp vMoveDown)
                let p = { p with view = v }
                p, ItemOperations(delete, sMoveUp, sMoveDown), ident
            do Seq.iter (mk >> arr.Add) inits

            let changesView =
                var.View
                |> View.Bind (fun arr ->
                    arr.ToArray()
                    |> Array.MapTreeReduce
                        (fun (p, _, _ as x) -> p.view |> View.Map (fun _ -> Seq.singleton x))
                        (View.Const Seq.empty)
                        (View.Map2 Seq.append)
                )

            let add x =
                arr.Add(mk x)
                Var.Update var id

            let adderView x =
                match x with
                | Failure _ -> ()
                | Success x -> add x
                Doc.Empty

            let out =
                var.View
                |> View.Bind (fun s ->
                    s.ToArray()
                    |> Array.MapTreeReduce
                        (fun (p, _, _) -> p.view |> View.Map (Result.Map Seq.singleton))
                        (View.Const (Success Seq.empty))
                        (View.Map2 (Result.Append Seq.append))
                )

            member this.View = out

            member this.Render (f: ItemOperations -> 'V) : Doc =
                changesView
                |> Doc.BindSeqCachedBy (fun (_, _, ident) -> ident) (fun (p, ops, _) ->
                    p.render (f ops) :> Doc
                )

            member this.Add (x: 'T) =
                add x

            member this.RenderAdder f =
                adder.render f
                |> Doc.Append (adder.view |> View.Map adderView |> Doc.EmbedView)

        [<Class>]
        type CollectionWithDefault<'T, 'V, 'W when 'W :> Doc> (p, inits, pInit, ``default``) =
            inherit Collection<'T, 'V, 'W, 'V, 'W> (p, inits, pInit)

            member this.Add() = this.Add ``default``

    [<Inline>]
    let (>>^) v f = fun g -> g (v f)

    let Create view (renderBuilder: _ -> _) =
        {
            id = Fresh.Id()
            view = view
            render = renderBuilder
        }

    let Render renderFunction p =
        p.render renderFunction
        |> Doc.Append (
            p.view
            |> View.Map (fun _ -> Doc.Empty)
            |> Doc.EmbedView
        )

    let RenderMany (c: Many.Collection<_,_,_,_,_>) f =
        c.Render f

    let RenderManyAdder (c: Many.Collection<_,_,_,_,_>) f =
        c.RenderAdder f

    let RenderPrimary (d: Dependent<_,_,_>) f =
        d.RenderPrimary f

    let RenderDependent (d: Dependent<_,_,_>) f =
        d.RenderDependent f

    let GetView (p: Form<_, _ -> _>) =
        p.view

    let Return value =
        {
            id = Fresh.Id()
            view = View.Const (Success value)
            render = id
        }

    let ReturnFailure () =
        {
            id = Fresh.Id()
            view = View.Const (Failure [])
            render = id
        }

    let YieldVar (var: IRef<_>) =
        {
            id = var.Id
            view = var.View |> View.Map Success
            render = fun r -> r var
        }

    let Yield value =
        YieldVar (Var.Create value)

    let YieldFailure () =
        let var = Var.Create JS.Undefined<_> :> IRef<_>
        let view = var.View
        {
            id = var.Id
            view = View.SnapshotOn (Failure []) view (view |> View.Map Success)
            render = fun r -> r var
        }

    let YieldOption init noneValue =
        let var = Var.Create (defaultArg init noneValue) :> IRef<_>
        {
            id = var.Id
            view = var.View |> View.Map (fun x ->
                Success (if x = noneValue then None else Some x))
            render = fun r -> r var
        }

    let Apply pf px =
        {
            id = Fresh.Id()
            view = View.Map2 Result.Apply pf.view px.view
            render = pf.render >> px.render
        }

    let ApJoin pf px =
        {
            id = Fresh.Id()
            view = View.Map2 Result.ApJoin pf.view px.view
            render = pf.render >> px.render
        }

    let WithSubmit p =
        let submitter = Submitter.Create p.view (Failure [])
        {
            id = Fresh.Id()
            view = submitter.View
            render = fun r -> p.render r submitter
        }

    let TransmitView p =
        {
            id = p.id
            view = p.view
            render = fun x -> p.render x p.view
        }

    let TransmitViewMapResult f p =
        {
            id = p.id
            view = p.view
            render = fun x -> p.render x (View.Map f p.view)
        }

    let TransmitViewMap f p =
        TransmitViewMapResult (Result.Map f) p

    let MapResult f p : Form<_, _ -> _> =
        {
            id = p.id
            view = View.Map f p.view
            render = p.render
        }

    let MapToResult f p =
        MapResult (Result.Bind f) p

    let Map f p =
        MapResult (Result.Map f) p

    let MapAsyncResult f p : Form<_, _ -> _> =
        {
            id = p.id
            view = View.MapAsync f p.view
            render = p.render
        }

    let MapToAsyncResult f p =
        let f x =
            match x with
            | Success x -> async { return! f x }
            | Failure m -> async { return Failure m }
        MapAsyncResult f p

    let MapAsync f p =
        let f x =
            match x with
            | Success x -> async { let! y = f x in return Success y }
            | Failure m -> async { return Failure m }
        MapAsyncResult f p

    let MapRenderArgs f p =
        {
            id = p.id
            view = p.view
            render = fun g -> g (p.render f)
        }

    let FlushErrors p =
        MapResult (function Failure _ -> Failure [] | x -> x) p

    let Run f p =
        Map (fun x -> f x; x) p

    let RunResult f p =
        MapResult (fun x -> f x; x) p

    [<JavaScript>]
    let ManyForm inits create p =
        let m = Many.Collection(p, inits, create)
        {
            id = Fresh.Id()
            view = m.View
            render = fun f -> f m
        }

    [<JavaScript>]
    let Many inits init p =
        let pInit = p init
        let m = Many.CollectionWithDefault(p, inits, pInit, init)
        {
            id = Fresh.Id()
            view = m.View
            render = fun f -> f m
        }

    let Dependent input output =
        let d = Dependent.Make input output
        {
            id = Fresh.Id()
            view = d.View
            render = fun f -> f d
        }

    type Builder =
        | Do

        member this.Bind(p, f) = Dependent p f

        member this.Return x = Return x

        member this.ReturnFrom (p: Form<_, _ -> _>) = p

        member this.Yield x = Yield x

        member this.YieldFrom (p: Form<_, _ -> _>) = p

        member this.Zero() = ReturnFailure()

[<JavaScript>]
module Validation =

    let Is pred msg p =
        p |> Form.MapResult (fun res ->
            match res with
            | Success x -> if pred x then res else Failure [ErrorMessage(p.id, msg)]
            | Failure _ -> res
        )

    let IsNotEmpty msg p =
        Is (fun x -> x <> "") msg p

    let IsMatch (re: string) msg p =
        Is (RegExp(re).Test) msg p

    let MapValidCheckedInput msg p =
        p |> Form.MapResult (fun res ->
            match res with
            | Success (CheckedInput.Valid (x, _)) -> Success x
            | Success _ -> Failure [ErrorMessage.Create(p, msg)]
            | Failure msgs -> Failure msgs
        )

[<JavaScript>]
[<AutoOpen>]
module Pervasives =

    let (<*>) pf px =
        Form.Apply pf px

    let (<*?>) pf px =
        Form.ApJoin pf px

[<JavaScript>]
module Attr =

    open WebSharper.UI.Next.Html
    open WebSharper.UI.Next.Client

    let SubmitterValidate (submitter: Submitter<_>) =
        Attr.Append
            (on.click (fun _ _ -> submitter.Trigger()))
            (attr.disabledDynPred (View.Const "disabled")
                (submitter.Input |> View.Map Result.IsFailure))

[<JavaScript>]
module Doc =

    open WebSharper.UI.Next.Html
    open WebSharper.UI.Next.Client

    let ButtonValidate caption attrs (submitter: Submitter<_>) =
        buttonAttr (Seq.append [|Attr.SubmitterValidate submitter|] attrs) [text caption]

    let ShowErrors (v: View<Result<'T>>) (f: list<ErrorMessage> -> Doc) =
        v.Doc(function
            | Success _ -> Doc.Empty
            | Failure msgs -> f msgs
        )

    let ShowSuccess (v: View<Result<'T>>) (f: 'T -> Doc) =
        v.Doc(function
            | Success x -> f x
            | Failure msgs -> Doc.Empty
        )

[<Extension; Sealed; JavaScript>]
type View =

    [<Extension>]
    static member Through (this: View<Result<'T>>, v: IRef<'U>) : View<Result<'T>> =
        this |> View.Map (fun x ->
            match x with
            | Success _ -> x
            | Failure msgs -> Failure (msgs |> List.filter (fun m -> m.Id = v.Id))
        )

    [<Extension>]
    static member Through (this: View<Result<'T>>, p: Form<'U, 'R>) : View<Result<'T>> =
        this |> View.Map (fun x ->
            match x with
            | Success _ -> x
            | Failure msgs -> Failure (msgs |> List.filter (fun m -> m.Id = p.id))
        )

    [<Extension; Inline>]
    static member ShowErrors (this: View<Result<'T>>, f: list<ErrorMessage> -> Doc) : Doc =
        Doc.ShowErrors this f

    [<Extension; Inline>]
    static member ShowSuccess (this: View<Result<'T>>, f: 'T -> Doc) : Doc =
        Doc.ShowSuccess this f
