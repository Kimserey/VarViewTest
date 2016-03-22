#I "../packages/"
#load "WebSharper.Warp/tools/reference-nover.fsx"

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

(*
    Starting to refactor using Lens
    
*)

[<JavaScript; AutoOpen>]
module Utils =
    let (<*>) f x = View.Apply f x

[<JavaScript>]
module Model =
    
    type Book = {
        Title: string
        Pages: Page list
    } with
        static member LensTitle (v: IRef<Book>) : IRef<string> =
            v.Lens (fun b -> b.Title) (fun b t -> { b with Title = t })

        static member LensPages (v: IRef<Book>) : IRef<Page list> =
            v.Lens (fun b -> b.Pages) (fun b p -> { b with Pages = p })
            
        static member LensPage n (v: IRef<Book>) : IRef<Page> =
            v.Lens
                (fun b -> b.Pages |> List.find (fun p -> p.Number = n))
                (fun b p -> { b with Pages = b.Pages |> List.map (fun p' -> if p'.Number = n then p else p') })

    and Page = {
        Number: int
        Content: string
        Comments: Comment list
    } with
        static member LensNumber (v: IRef<Page>) : IRef<int> =
            v.Lens (fun c -> c.Number) (fun c n -> { c with Number = n })

        static member LensContent (v: IRef<Page>) : IRef<string> =
            v.Lens (fun c -> c.Content) (fun c cont -> { c with Content = cont })    
            
        static member LensComments (v: IRef<Page>) : IRef<Comment list> =
            v.Lens (fun c -> c.Comments) (fun p c -> { p with Comments = c })
            
        static member LensComment n (v: IRef<Page>) : IRef<Comment> =
            v.Lens 
                (fun p -> p.Comments |> List.find (fun p -> p.Number = n)) 
                (fun c com -> { c with Comments = c.Comments |> List.map (fun c' -> if c'.Number = n then com else c') })

    and Comment = {
        Number: int
        Content: string
    } with
        static member LensNumber (v: IRef<Comment>) : IRef<int> =
            v.Lens (fun c -> c.Number) (fun c n -> { c with Number = n })

        static member LensContent (v: IRef<Comment>) : IRef<string> =
            v.Lens (fun c -> c.Content) (fun c cont -> { c with Content = cont })    


[<JavaScript>]
module Render =
    open Model
    
    type Comment with
        static member Render (comment: Comment) =
            let textDash =
                spanAttr [ attr.style "margin-left: 3em;" ] [ text "-" ]
            let text txt =
                spanAttr [ attr.style "margin-left: 4em;" ] [ text txt ]

            div [ textDash; br []
                  text ("number: " +  string comment.Number); br []
                  text ("content: " + comment.Content) ]

    type Page with
        static member Render (page: Page) =
            let textDash =
                spanAttr [ attr.style "margin-left: 1em;" ] [ text "-" ]
            let text txt =
                spanAttr [ attr.style "margin-left: 2em;" ] [ text txt ]

            div [ textDash; br []
                  text ("number: " +  string page.Number); br []
                  text ("content: " + page.Content); br []
                  text "comments: "; br []
                  page.Comments
                  |> List.map Comment.Render
                  |> Seq.cast
                  |> Doc.Concat ]

    type Book with
        static member Render (book: Book) =
            div [ text ("title: " + book.Title); br []
                  text "pages: "; br []
                  book.Pages
                  |> List.map Page.Render 
                  |> Seq.cast 
                  |> Doc.Concat ]

[<JavaScript>]
module Builder =
    open Model

    let buildComment book (p: Page) (c: Comment) =
        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ] 
                            [ label [ text "Number" ]
                              Doc.IntInputUnchecked 
                                [ attr.``class`` "form-control"; attr.disabled "true" ] 
                                (Book.LensPage p.Number book
                                 |> Page.LensComment c.Number 
                                 |> Comment.LensNumber) ]

                  divAttr [ attr.``class`` "form-group" ] 
                            [ label [ text "Content" ]
                              Doc.Input 
                                [ attr.``class`` "form-control" ]  
                                (Book.LensPage p.Number book
                                 |> Page.LensComment c.Number 
                                 |> Comment.LensContent) ] ] :> Doc

    let buildPage trigger book (p: Page) =   
        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ] 
                            [ label [ text "Number" ]
                              Doc.IntInputUnchecked [ attr.``class`` "form-control"; attr.disabled "true" ] (Page.LensNumber (Book.LensPage p.Number book)) ]
                  divAttr [ attr.``class`` "form-group" ] 
                            [ label [ text "Content" ]
                              Doc.Input [ attr.``class`` "form-control" ] (Page.LensContent (Book.LensPage p.Number book)) ]
                  divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Comments" ]
                            (Book.LensPage p.Number book
                             |> Page.LensComments).View
                            |> Doc.BindView (fun comms ->
                                [ Doc.Button "-"
                                    [ attr.``class`` "btn btn-default" ] 
                                    (fun () -> if comms.Length >= 0 then (Book.LensPage p.Number book |> Page.LensComments).Set (comms.[0..comms.Length - 2])) :> Doc
                                                                
                                  Doc.Button "+" 
                                    [ attr.``class`` "btn btn-default" ] 
                                    (fun () -> (Book.LensPage p.Number book |> Page.LensComments).Set (comms @ [ { Number = comms.Length; Content = "" } ])) :> Doc ]
                                |> Doc.Concat)
                               
                            (Book.LensPage p.Number book
                             |> Page.LensComments).View
                            |> View.SnapshotOn p.Comments trigger
                            |> Doc.BindView (List.map (buildComment book p) >> Doc.Concat) ] ] :> Doc

    let buildBook trigger book =
        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ]
                          [ label [ text "Title" ]
                            Doc.Input [ attr.``class`` "form-control" ] (Book.LensTitle book) ]
                  divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Pages" ]
                            
                            (Book.LensPages book).View
                            |> Doc.BindView (fun pages ->
                                [ Doc.Button "-"
                                     [ attr.``class`` "btn btn-default" ] 
                                     (fun () -> if pages.Length >= 0 then (Book.LensPages book).Set (pages.[0..pages.Length - 2])) :> Doc
                                  
                                  Doc.Button "+" 
                                     [ attr.``class`` "btn btn-default" ] 
                                     (fun () -> (Book.LensPages book).Set (pages @ [ { Number = pages.Length; Content = ""; Comments = [] } ])) :> Doc ]
                                |> Doc.Concat)
                
                            (Book.LensPages book).View
                            |> View.SnapshotOn (book.Get().Pages) trigger
                            |> Doc.BindView (List.map (buildPage trigger book) >> Doc.Concat) ] ]

[<JavaScript>]
module Client =
    open Model
    open Render
    open Builder

    let main() =
        let trigger =
            Var.Create ()
        
        let book = 
            Var.Create { Title = "New Book"
                         Pages = [] }

        let container content =
            divAttr [ attr.style "position:fixed; height: 85%; width: 48%; top: 10%; overflow-y: scroll;"
                      attr.``class`` "well" ]
                    [ content ]

        divAttr [ attr.``class`` "container-fluid" ]
                [ h1Attr [ attr.``class`` "text-center" ] 
                         [ text "Book - Live preview" ]
                  divAttr [ attr.``class`` "row" ]
                          [ divAttr [ attr.``class`` "col-xs-6" ]
                                    [ [ Doc.Button "Generate" [ attr.``class`` "btn btn-primary" ] (Var.Set trigger)
                                        buildBook trigger.View book ]
                                      |> Seq.cast
                                      |> Doc.Concat
                                      |> container ]
                            
                            divAttr [ attr.``class`` "col-xs-6" ]
                                    [ book.View 
                                      |> View.SnapshotOn book.Value trigger.View
                                      |> Doc.BindView Book.Render 
                                      |> container ] ] ]

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