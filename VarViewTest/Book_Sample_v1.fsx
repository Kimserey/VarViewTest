#I "../packages/"
#load "WebSharper.Warp/tools/reference-nover.fsx"

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

[<JavaScript; AutoOpen>]
module Utils =
    let (<*>) f x = View.Apply f x

[<JavaScript>]
module Model =
    
    type Book = {
        Title: string
        Pages: Page list
    }
    and Page = {
        Number: int
        Content: string
        Comments: Comment list
    }
    and Comment = {
        Number: int
        Content: string
    }

    type ReactiveBook = {
        Title: Var<string>
        Pages: Var<ReactivePage list>
    }
    and ReactivePage = {
        Number: Var<int>
        Content: Var<string>
        Comments: Var<ReactiveComment list>
    }
    and ReactiveComment = {
        Number: Var<int>
        Content: Var<string>
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
                  text ("comment:"); br []
                  div [  page.Comments 
                         |> List.map Comment.Render
                         |> Seq.cast
                         |> Doc.Concat ]
                   ]

    type Book with
        static member Render (book: Book) =
            div [ text ("title: " + book.Title); br []
                  text "pages: "
                  book.Pages
                  |> List.map Page.Render 
                  |> Seq.cast 
                  |> Doc.Concat ]

[<JavaScript>]
module Views =
    open Model
    
    type ReactiveComment with
        static member View comment: View<Comment> =
            View.Const (fun n c -> 
                { Number = n
                  Content = c })
            <*> comment.Number.View
            <*> comment.Content.View

    type ReactivePage with
        static member View (page: ReactivePage): View<Page> =
            View.Const (fun n c com-> 
                { Number = n
                  Content = c
                  Comments = com |> Seq.toList })
            <*> page.Number.View
            <*> page.Content.View
            <*> (page.Comments.View
                 |> View.Map (fun comments -> 
                        comments 
                        |> List.map ReactiveComment.View 
                        |> View.Sequence) 
                 |> View.Join)

    type ReactiveBook with
        static member View book: View<Book> =
            View.Const (fun t p -> 
                { Title = t
                  Pages = p |> Seq.toList })
            <*> book.Title.View
            <*> (book.Pages.View 
                 |> View.Map (fun pages -> 
                        pages 
                        |> List.map ReactivePage.View 
                        |> View.Sequence) 
                 |> View.Join)

[<JavaScript>]
module Builder =
    open Model

    type ReactiveComment with
        static member RenderBuilder (rv: ReactiveComment) =
            divAttr [ attr.``class`` "well" ]
                    [ divAttr [ attr.``class`` "form-group" ]
                              [ label [ text "Number" ]
                                Doc.IntInputUnchecked [ attr.``class`` "form-control"; attr.disabled "true" ] rv.Number ]
                      
                      divAttr [ attr.``class`` "form-group" ]
                              [ label [ text "Content" ]
                                Doc.Input [ attr.``class`` "form-control" ] rv.Content ] ] :> Doc

    type ReactivePage with
        static member RenderBuilder (rv: ReactivePage) =
            divAttr [ attr.``class`` "well" ]
                    [ divAttr [ attr.``class`` "form-group" ]
                              [ label [ text "Number" ]
                                Doc.IntInputUnchecked [ attr.``class`` "form-control"; attr.disabled "true" ] rv.Number ]
                      
                      divAttr [ attr.``class`` "form-group" ]
                              [ label [ text "Content" ]
                                Doc.Input [ attr.``class`` "form-control" ] rv.Content ]
                                
                      divAttr [ attr.``class`` "form-group" ] 
                              [ label [ text "Comments" ]
                                Doc.Button "-" [ attr.``class`` "btn btn-default" ] (fun () -> if rv.Comments.Value.Length > 0 then Var.Set rv.Comments (rv.Comments.Value |> List.take (rv.Comments.Value.Length - 1)))
                                Doc.Button "+" [ attr.``class`` "btn btn-default" ] (fun () -> Var.Set rv.Comments (rv.Comments.Value @ [ { Number = Var.Create rv.Comments.Value.Length; Content = Var.Create "" } ])) 
                                
                                rv.Comments.View
                                |> Doc.BindView (List.map ReactiveComment.RenderBuilder >> Doc.Concat) ] ] :> Doc
    
    type ReactiveBook with
        static member RenderBuilder (rv: ReactiveBook) =
            divAttr [ attr.``class`` "well" ]
                    [ divAttr [ attr.``class`` "form-group" ]
                              [ label [ text "Title" ]
                                Doc.Input [ attr.``class`` "form-control" ] rv.Title ]
                    
                      divAttr [ attr.``class`` "form-group" ] 
                              [ label [ text "Pages" ]
                                Doc.Button "-" [ attr.``class`` "btn btn-default" ] (fun () -> if rv.Pages.Value.Length > 0 then Var.Set rv.Pages (rv.Pages.Value |> List.take (rv.Pages.Value.Length - 1)))
                                Doc.Button "+" [ attr.``class`` "btn btn-default" ] (fun () -> Var.Set rv.Pages (rv.Pages.Value @ [ { Number = Var.Create rv.Pages.Value.Length; Content = Var.Create ""; Comments = Var.Create [] } ])) 
                                
                                rv.Pages.View
                                |> Doc.BindView (List.map ReactivePage.RenderBuilder >> Doc.Concat) ] ]

[<JavaScript>]
module Client =
    open Model
    open Views
    open Render
    open Builder

    let main() =
        let rvBook = 
            { Title = Var.Create "New book"
              Pages = Var.Create [ { Number = Var.Create 0
                                     Content = Var.Create "A new page."
                                     Comments = Var.Create [] } ] }
        
        let container content =
            divAttr [ attr.style "position:fixed; height: 85%; width: 48%; top: 10%; overflow-y: scroll;"
                      attr.``class`` "well" ]
                    [ content ]

        divAttr [ attr.``class`` "container-fluid" ]
                [ h1Attr [ attr.``class`` "text-center" ] 
                         [ text "Book - Live preview" ]
                  divAttr [ attr.``class`` "row" ]
                          [ divAttr [ attr.``class`` "col-xs-6" ]
                                    [ rvBook
                                      |> ReactiveBook.RenderBuilder 
                                      |> container ]
                            divAttr [ attr.``class`` "col-xs-6" ]
                                    [ rvBook
                                      |> ReactiveBook.View
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