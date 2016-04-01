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

    type Pet =
    | Dog of name:string * owner:int
    | Cat of name:string
        with
            static member Name =
                function
                | Dog (n, _) -> n
                | Cat n -> n

    let main () =
        let listModel = 
            ListModel.Create 
                Pet.Name 
                [ Dog ("muffin", 1)
                  Dog ("cupcake", 1)
                  Cat "muffin" ]
         
        let cupcakeOwnerRef =
            listModel.LensInto 
                (function 
                 | Dog ("cupcake", owner) -> owner 
                 | _ -> failwith "Supposed to ref cupcake") 
                (fun p v -> 
                    match p with
                    | Dog ("cupcake", _) -> Dog ("cupcake", v) 
                    | _ -> failwith "Supposed to ref cupcake") 
                "cupcake"

        div 
            [ listModel.View
              |> Doc.BindSeqCached(
                    function
                    | Dog (key, owner) ->
                        div [ text key; br[]; text (string owner) ]
                    | Cat key ->
                        div [ text key ])
              br[]
              Doc.IntInputUnchecked [] cupcakeOwnerRef ]


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