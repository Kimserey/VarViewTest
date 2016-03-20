#I "../packages/"
#load "WebSharper.Warp/tools/reference-nover.fsx"

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

[<JavaScript>]
module Client =
    
    type Item = {
        Key: int
        Text: string
    }

    type ToDo = {
        Key: int
        Content: string
        State: State
        Items: Item list
    } 
    and State =
        | Done
        | NotStarted

    let main() =
        let list = 
            ListModel.Create (fun i -> i.Key) [ { Key = 0; Content = "Buy bread"; State = NotStarted; Items = [] }; { Key = 1; Content = "Wash the clothes"; State = NotStarted; Items = [] } ]
        let lensState = 
            list.LensInto (fun t -> t.State) (fun t s -> { t with State = s })
        let lensTextItem itemKey =
            list.LensInto 
                (fun t -> t.Items 
                          |> List.tryFind (fun i -> i.Key = itemKey)
                          |> Option.map (fun i -> i.Text)) 
                (fun t i -> t.Items |> List.tryFind (fun i -> i.Key = itemKey)
                            |> Option. |> Option.map (fun i -> i.)

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
                        ||> View.Map2 (fun l c -> Doc.Button "Add" [] (fun () -> list.Add { Key = l; Content = c; State = State.NotStarted; Items = [] }))
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