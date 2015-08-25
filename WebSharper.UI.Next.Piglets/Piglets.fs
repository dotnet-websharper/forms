namespace WebSharper.UI.Next.Piglets

open System.Runtime.CompilerServices
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Notation

[<JavaScript; Sealed>]
type ErrorMessage(id: int, message: string) =

    [<Inline>]
    member this.Id = id

    [<Inline>]
    member this.Text = message

[<JavaScript>]
module Fresh =

    let lastId = ref -1

    let Id() =
        decr lastId
        !lastId

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
type Piglet<'T, 'R> =
    {
        Id : int
        View : View<Result<'T>>
        Render : 'R
    }

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

[<Sealed; JavaScript>]
type Chooser<'Out, 'In, 'U, 'V, 'W, 'X when 'In : equality and 'X :> Doc> (chooser: Piglet<'In, 'U -> 'V>, choice: 'In -> Piglet<'Out, 'W -> 'X>) =

    let choice = memoize choice

    let pOut =
        chooser.View
        |> View.Map (function
            | Success i -> Success (choice i)
            | Failure m -> Failure m)

    let out =
        pOut |> View.Bind (function
            | Success p -> p.View
            | Failure m -> View.Const (Failure m))
        
    member this.View : View<Result<'Out>> = out

    member this.Chooser (f: 'U) : 'V =
        chooser.Render f

    member this.Choice (f: 'W) : Doc =
        pOut
        |> View.Map (function
            | Success p -> p.Render f :> Doc
            | Failure _ -> Doc.Empty)
        |> Doc.EmbedView

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

    module Array =

        let MapReduce (f: 'A -> 'B) (z: 'B) (re: 'B -> 'B -> 'B) (a: 'A[]) : 'B =
            let rec loop off len =
                match len with
                | n when n <= 0 -> z
                | 1 when off >= 0 && off < a.Length ->
                    f a.[off]
                | n ->
                    let l2 = len / 2
                    let a = loop off l2
                    let b = loop (off + l2) (len - l2)
                    re a b
            loop 0 a.Length

    module Fresh =

        let Int =
            let x = ref 0
            fun () ->
                incr x
                !x

    type Collection<'T, 'V, 'W, 'Y, 'Z when 'W :> Doc and 'Z :> Doc> (p : 'T -> Piglet<'T, 'V -> 'W>, inits: seq<'T>, adder : Piglet<'T, 'Y -> 'Z>) =
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
            let v = View.Map2 (fun x () -> x) p.View (View.Map2 (fun () () -> ()) vMoveUp vMoveDown)
            let p = { p with View = v }
            p, ItemOperations(delete, sMoveUp, sMoveDown), ident
        do Seq.iter (mk >> arr.Add) inits

        let changesView =
            var.View
            |> View.Bind (fun arr ->
                arr.ToArray()
                |> Array.MapReduce
                    (fun (p, _, _ as x) -> p.View |> View.Map (fun _ -> Seq.singleton x))
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
                |> Array.MapReduce
                    (fun (p, _, _) -> p.View |> View.Map (Result.Map Seq.singleton))
                    (View.Const (Success Seq.empty))
                    (View.Map2 (Result.Append Seq.append))
            )

        member this.View = out

        member this.Render (f: ItemOperations -> 'V) : Doc =
            changesView
            |> Doc.ConvertBy (fun (_, _, ident) -> ident) (fun (p, ops, _) ->
                p.Render (f ops) :> Doc
            )

        member this.Add (x: 'T) =
            add x

        member this.RenderAdder f =
            adder.Render f
            |> Doc.Append (adder.View |> View.Map adderView |> Doc.EmbedView)

    [<Class>]
    type CollectionWithDefault<'T, 'V, 'W when 'W :> Doc> (p, inits, pInit, ``default``) =
        inherit Collection<'T, 'V, 'W, 'V, 'W> (p, inits, pInit)

        member this.Add() = this.Add ``default``

[<JavaScript>]
module Piglet =

    [<Inline>]
    let (>>^) v f = fun g -> g (v f)

    let Create view (renderBuilder: _ -> _) =
        {
            Id = Fresh.Id()
            View = view
            Render = renderBuilder
        }

    let Render renderFunction p =
        p.Render renderFunction
        |> Doc.Append (
            p.View
            |> View.Map (fun _ -> Doc.Empty)
            |> Doc.EmbedView
        )

    let GetView (p: Piglet<_, _ -> _>) =
        p.View

    let Return value =
        {
            Id = Fresh.Id()
            View = View.Const (Success value)
            Render = id
        }

    let ReturnFailure () =
        {
            Id = Fresh.Id()
            View = View.Const (Failure [])
            Render = id
        }

    let Yield value =
        let var = Var.Create value
        {
            Id = Var.GetId var
            View = var.View |> View.Map Success
            Render = fun r -> r var
        }

    let YieldFailure () =
        let var = Var.Create Unchecked.defaultof<_>
        let view = var.View
        {
            Id = Var.GetId var
            View = View.SnapshotOn (Failure []) view (view |> View.Map Success)
            Render = fun r -> r var
        }

    let YieldOption init noneValue =
        let var = Var.Create (defaultArg init noneValue)
        {
            Id = Var.GetId var
            View = var.View |> View.Map (fun x ->
                Success (if x = noneValue then None else Some x))
            Render = fun r -> r var
        }

    let Apply pf px =
        {
            Id = Fresh.Id()
            View = View.Map2 Result.Apply pf.View px.View
            Render = pf.Render >> px.Render
        }

    let ApJoin pf px =
        {
            Id = Fresh.Id()
            View = View.Map2 Result.ApJoin pf.View px.View
            Render = pf.Render >> px.Render
        }

    let WithSubmit p =
        let submitter = Submitter.Create p.View (Failure [])
        {
            Id = Fresh.Id()
            View = submitter.View
            Render = fun r -> p.Render r submitter
        }

    let TransmitView p =
        {
            Id = p.Id
            View = p.View
            Render = fun x -> p.Render x p.View
        }

    let TransmitViewMapResult f p =
        {
            Id = p.Id
            View = p.View
            Render = fun x -> p.Render x (View.Map f p.View)
        }

    let TransmitViewMap f p =
        TransmitViewMapResult (Result.Map f) p

    let MapResult f p : Piglet<_, _ -> _> =
        {
            Id = p.Id
            View = View.Map f p.View
            Render = p.Render
        }

    let MapToResult f p =
        MapResult (Result.Bind f) p

    let Map f p =
        MapResult (Result.Map f) p

    let MapAsyncResult f p : Piglet<_, _ -> _> =
        {
            Id = p.Id
            View = View.MapAsync f p.View
            Render = p.Render
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
            Id = p.Id
            View = p.View
            Render = fun g -> g (p.Render f)
        }

    let FlushErrors p =
        MapResult (function Failure _ -> Failure [] | x -> x) p

    let Run f p =
        Map (fun x -> f x; x) p

    let RunResult f p =
        MapResult (fun x -> f x; x) p

    [<JavaScript>]
    let ManyPiglet inits create p =
        let m = Many.Collection(p, inits, create)
        {
            Id = Fresh.Id()
            View = m.View
            Render = fun f -> f m
        }

    [<JavaScript>]
    let Many inits init p =
        let pInit = p init
        let m = Many.CollectionWithDefault(p, inits, pInit, init)
        {
            Id = Fresh.Id()
            View = m.View
            Render = fun f -> f m
        }

    let Choose input output =
        let c = Chooser(input, output)
        {
            Id = Fresh.Id()
            View = c.View
            Render = fun f -> f c
        }

    type Builder =
        | Do

        member this.Bind(p, f) = Choose p f

        member this.Return x = Return x

        member this.ReturnFrom (p: Piglet<_, _ -> _>) = p

        member this.Yield x = Yield x

        member this.YieldFrom (p: Piglet<_, _ -> _>) = p

        member this.Zero() = ReturnFailure()

[<JavaScript>]
module Validation =

    let Is pred msg p =
        p |> Piglet.MapResult (fun res ->
            match res with
            | Success x -> if pred x then res else Failure [ErrorMessage(p.Id, msg)]
            | Failure _ -> res
        )

    let IsNotEmpty msg p =
        Is (fun x -> x <> "") msg p

    let IsMatch (re: string) msg p =
        Is (RegExp(re).Test) msg p

[<JavaScript>]
[<AutoOpen>]
module Pervasives =

    let (<*>) pf px =
        Piglet.Apply pf px

    let (<*?>) pf px =
        Piglet.ApJoin pf px

[<JavaScript>]
module Doc =

    open WebSharper.UI.Next.Html
    open WebSharper.UI.Next.Client

    let ButtonValidate caption attrs (submitter: Submitter<_>) =
        let attrs =
            attrs |> Seq.append [
                attr.disabledDynPred (View.Const "disabled")
                    (submitter.Input |> View.Map Result.IsFailure)
            ]
        Doc.Button caption attrs submitter.Trigger

[<Extension; Sealed; JavaScript>]
type View =

    [<Extension>]
    static member Through (this: View<Result<'T>>, v: Var<'U>) : View<Result<'T>> =
        this |> View.Map (fun x ->
            match x with
            | Success _ -> x
            | Failure msgs -> Failure (msgs |> List.filter (fun m -> m.Id = Var.GetId v))
        )

    [<Extension>]
    static member Through (this: View<Result<'T>>, p: Piglet<'U, 'R>) : View<Result<'T>> =
        this |> View.Map (fun x ->
            match x with
            | Success _ -> x
            | Failure msgs -> Failure (msgs |> List.filter (fun m -> m.Id = p.Id))
        )
