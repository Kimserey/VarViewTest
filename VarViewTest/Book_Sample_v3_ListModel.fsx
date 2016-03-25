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

[<JavaScript>]
module Model =
    
    type Book = {
        Title: string
        Pages: ListModel<int, Page>
    } with
        static member LensTitle (v: IRef<Book>) : IRef<string> =
            v.Lens (fun b -> b.Title) (fun b t -> { b with Title = t })

    and Page = {
        Number: int
        Content: string
        Comments: ListModel<int, Comment>
    } with
        static member LensIntoContent n (v: Book) : IRef<string> =
            v.Pages.LensInto (fun p -> p.Content) (fun p c -> { p with Content = c }) n
    
    and Comment = {
        Number: int
        Content: string
    } with
        static member LensIntoContent n (v: Page) : IRef<string> =
            v.Comments.LensInto (fun c -> c.Content) (fun c c' -> { c with Content = c' }) n

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

                  page.Comments.View
                  |> Doc.BindSeqCached Comment.Render]

    type Book with
        static member Render title pages =
            div [ Doc.BindView (fun title -> text ("title: " + title)) title; br []
                  text "pages: "; br []

                  pages
                  |> Doc.BindSeqCached Page.Render ]

[<JavaScript>]
module Builder =
    open Model
    
    let makeTrigger() = 
        let t = Var.Create ()
        Var.Set t, t.View

    let buildComment book (p: Page) (c: Comment) =
        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Number" ]
                            inputAttr [ attr.value (string c.Number)
                                        attr.``class`` "form-control"
                                        attr.disabled "true" ] [] ]

                  divAttr [ attr.``class`` "form-group" ] 
                            [ label [ text "Content" ]
                              Doc.Input [ attr.``class`` "form-control" ]  (Comment.LensIntoContent c.Number p) ] ] :> Doc

    let buildPage book (p: Page) =   
    
        let (refresh, view) =
            makeTrigger()

        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Number" ]
                            inputAttr [ attr.value (string p.Number)
                                        attr.``class`` "form-control"
                                        attr.disabled "true" ] [] ]

                  divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Content" ]
                            Doc.Input [ attr.``class`` "form-control" ] 
                                      (Page.LensIntoContent p.Number book) ]

                  divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Comments" ]

                            p.Comments.LengthAsView
                            |> Doc.BindView (fun l ->
                                [ Doc.Button "-" 
                                             [ attr.``class`` "btn btn-default" ] 
                                             (fun () -> 
                                                if l >= 0 then
                                                    p.Comments.RemoveByKey(l - 1)
                                                    refresh()) :> Doc
                                  Doc.Button "+" 
                                             [ attr.``class`` "btn btn-default" ] 
                                             (fun () -> 
                                                p.Comments.Add { Number = l; Content = "" }
                                                refresh()) :> Doc ]
                                |> Doc.Concat)
                               
                            p.Comments.View
                            |> View.SnapshotOn p.Comments.Value view
                            |> Doc.BindSeqCached (buildComment book p) ] ] :> Doc

    let buildBook title (book: Book) =

        let (refresh, view) =
            makeTrigger()

        divAttr [ attr.``class`` "well" ]
                [ divAttr [ attr.``class`` "form-group" ]
                          [ label [ text "Title" ]
                            Doc.Input [ attr.``class`` "form-control" ] title ]
                  divAttr [ attr.``class`` "form-group" ] 
                          [ label [ text "Pages" ]
                            
                            book.Pages.LengthAsView
                            |> Doc.BindView (fun l ->
                                [ Doc.Button "-" 
                                             [ attr.``class`` "btn btn-default" ] 
                                             (fun () -> 
                                                if l >= 0 then
                                                    book.Pages.RemoveByKey (l - 1)
                                                    refresh()) :> Doc
                                  Doc.Button "+" 
                                             [ attr.``class`` "btn btn-default" ] 
                                             (fun () -> 
                                                book.Pages.Add { Number = l; Content = ""; Comments = ListModel.Create (fun c -> c.Number) [] }
                                                refresh()) :> Doc ]
                                |> Doc.Concat)
                
                            book.Pages.View
                            |> View.SnapshotOn book.Pages.Value view
                            |> Doc.BindSeqCached (buildPage book) ] ]

[<JavaScript>]
module Client =
    open Model
    open Render
    open Builder

    let main() =
        
        let book = 
            { Title = "New Book"
              Pages = ListModel.Create (fun p -> p.Number) [] }

        let title = Var.Create "New book"

        let container content =
            divAttr [ attr.style "position:fixed; height: 85%; width: 48%; top: 10%; overflow-y: scroll;"
                      attr.``class`` "well" ]
                    [ content ]

        divAttr [ attr.``class`` "container-fluid" ]
                [ h1Attr [ attr.``class`` "text-center" ] 
                         [ text "Book - Live preview" ]
                  divAttr [ attr.``class`` "row" ]
                          [ divAttr [ attr.``class`` "col-xs-6" ]
                                    [ [ buildBook title book ]
                                      |> Seq.cast
                                      |> Doc.Concat
                                      |> container ]
                            
                            divAttr [ attr.``class`` "col-xs-6" ]
                                    [ (title.View, book.Pages.View)
                                      ||> Book.Render
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