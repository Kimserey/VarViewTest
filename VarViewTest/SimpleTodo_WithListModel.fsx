#I "../packages/"
#load "WebSharper.Warp/tools/reference-nover.fsx"

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

(*

    Demonstration of usage of LensInto.
    Allows us to make a IRef from a member of an item in a ListModel.

*)
[<JavaScript>]
module Client =
    
    type ToDo = {
        Key: int
        Content: string
        State: State
    } 
    and State =
        | Done
        | NotStarted

    let main() =
        let list = 
            ListModel.Create (fun i -> i.Key) [ { Key = 0; Content = "Buy bread"; State = NotStarted }; { Key = 1; Content = "Wash the clothes"; State = NotStarted } ]
        let lensState = 
            list.LensInto (fun t -> t.State) (fun t s -> { t with State = s })

        let newContent = Var.Create ""

        divAttr [ attr.``class`` "container" ]
                [ p [ table [ list.View
                               |> Doc.BindSeqCached (fun t ->
                                     match t.State with
                                     | Done       -> tr [ td [ text (sprintf "✓ %s" t.Content) ]
                                                          td [ Doc.Button "Mark as not started" [] (fun () -> (lensState t.Key).Set State.NotStarted) ] ]
                                     | NotStarted -> tr [ td [ text (sprintf "- %s" t.Content) ]
                                                          td [ Doc.Button "Mark as done" [] (fun () -> (lensState t.Key).Set State.Done) ] ]) ] ] 
                  div [ Doc.Input [] newContent
                        (list.LengthAsView, newContent.View)
                        ||> View.Map2 (fun l c -> Doc.Button "Add" [] (fun () -> list.Add { Key = l; Content = c; State = State.NotStarted }))
                        |> Doc.EmbedView ] ]

module Server =

    type Page = { Body: Doc list }

    let template =
        Content.Template<Page>(__SOURCE_DIRECTORY__ + "/index.html")
            .With("body", fun x -> x.Body)
    
    let site =
        Application.SinglePage (fun _ ->
            Content.WithTemplate template
                { Body = [ client <@ Client.main() @> ] })

do Warp.RunAndWaitForInput Server.site |> ignore