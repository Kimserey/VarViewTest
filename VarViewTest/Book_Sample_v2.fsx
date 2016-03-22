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
                (fun b p -> 
                    { b with 
                        Pages = b.Pages |> List.map (fun p' ->
                            if p'.Number = n then p
                            else p')
                    })
    and Page = {
        Number: int
        Content: string
        Comments: Comment list
    } with
        static member LensNumber (v: IRef<Comment>) : IRef<int> =
            v.Lens (fun c -> c.Number) (fun c n -> { c with Number = n })

        static member LensContent (v: IRef<Comment>) : IRef<string> =
            v.Lens (fun c -> c.Content) (fun c cont -> { c with Content = cont })    

    and Comment = {
        Number: int
        Content: string
    }

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
module Client =
    open Model


    let main() =
        let title = 
            Var.Create "New book"

        let pages =
            ListModel.Create (fun p -> p.Number) []
        
        let container content =
            divAttr [ attr.style "position:fixed; height: 85%; width: 48%; top: 10%; overflow-y: scroll;"
                      attr.``class`` "well" ]
                    [ content ]

        divAttr [ attr.``class`` "container-fluid" ]
                [ h1Attr [ attr.``class`` "text-center" ] 
                         [ text "Book - Live preview" ]
                  divAttr [ attr.``class`` "row" ]
                          [ divAttr [ attr.``class`` "col-xs-6" ]
                                    [ divAttr [ attr.``class`` "well" ]
                                              [ divAttr [ attr.``class`` "form-group" ]
                                                        [ label [ text "Title" ]
                                                          Doc.Input [ attr.``class`` "form-control" ] title ]
                                                divAttr [ attr.``class`` "form-group" ] 
                                                        [ label [ text "Elements" ]
//                                                           
//                                                          pages.LengthAsView
//                                                          |> Doc.BindView (fun l ->
//                                                              [ Doc.Button "-"
//                                                                   [ attr.``class`` "btn btn-default" ] 
//                                                                   (fun () -> if l >= 0 then pages.RemoveByKey(View.Const l)) :> Doc
//                                                                
//                                                                Doc.Button "+" 
//                                                                   [ attr.``class`` "btn btn-default" ] 
//                                                                   (fun () -> pages.Add { Number = View.Const (l + 1); Content = View.Const "" }) :> Doc ]
//                                                              |> Doc.Concat)
                               
//                                                          pages.View
//                                                          |> Doc.BindView (List.map ReactivePage.RenderBuilder >> Doc.Concat)
                                                          ] ] ]
                            divAttr [ attr.``class`` "col-xs-6" ]
                                    [ ] ] ]
//                                      rvBook
//                                      |> ReactiveBook.View
//                                      |> Doc.BindView Book.Render
//                                      |> container ] ] ]

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