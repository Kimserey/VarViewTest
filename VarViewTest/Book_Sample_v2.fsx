//#I "../packages/"
//#load "WebSharper.Warp/tools/reference-nover.fsx"
//
//open WebSharper
//open WebSharper.JavaScript
//open WebSharper.Sitelets
//open WebSharper.UI.Next
//open WebSharper.UI.Next.Html
//open WebSharper.UI.Next.Client
//
//(*
//    Starting to refactor all the Var into View  
//*)
//
//[<JavaScript; AutoOpen>]
//module Utils =
//    let (<*>) f x = View.Apply f x
//
//[<JavaScript>]
//module Model =
//    
//    type Book = {
//        Title: string
//        Pages: Page list
//    } with
//        static member Render (book: Book) =
//            div [ text ("title: " + book.Title); br []
//                  text "pages: "
//                  book.Pages
//                  |> List.map Page.Render 
//                  |> Seq.cast 
//                  |> Doc.Concat ]
//
//    and Page = {
//        Number: int
//        Content: string
//    } with
//        static member Render (page: Page) =
//            let textDash =
//                spanAttr [ attr.style "margin-left: 1em;" ] [ text "-" ]
//            let text txt =
//                spanAttr [ attr.style "margin-left: 2em;" ] [ text txt ]
//
//            div [ textDash; br []
//                  text ("number: " +  string page.Number); br []
//                  text ("content: " + page.Content) ]
//
//    type ReactiveBook = {
//        Title: View<string>
//        Pages: View<ReactivePage seq>
//    } with
//        static member View book: View<Book> =
//            View.Const (fun t p -> { Title = t; Pages = p |> Seq.toList })
//            <*> book.Title
//            <*> (book.Pages
//                 |> View.Map (fun pages -> 
//                        pages 
//                        |> List.map ReactivePage.View 
//                        |> View.Sequence) 
//                 |> View.Join)
//
//    and ReactivePage = {
//        Number: View<int>
//        Content: View<string>
//    } with
//        static member View page: View<Page> =
//            View.Const (fun n c -> { Number = n; Content = c })
//            <*> page.Number
//            <*> page.Content           
//
//[<JavaScript>]
//module Builder =
//    open Model
//
//    type ReactivePage with
//        static member RenderBuilder (rv: ReactivePage) =
//            let number = Var.Create 0
//            let content = Var.Create ""
//            divAttr [ attr.``class`` "well" ]
//                    [ divAttr [ attr.``class`` "form-group" ]
//                              [ label [ text "Number" ]
//                                Doc.IntInputUnchecked [ attr.``class`` "form-control" ] number ]
//                      
//                      divAttr [ attr.``class`` "form-group" ]
//                              [ label [ text "Content" ]
//                                Doc.Input [ attr.``class`` "form-control" ] content ] ] :> Doc
//    
////    type ReactiveBook with
////        static member RenderBuilder (rv: ReactiveBook) =
////            let title = Var.Create ""
////
////            divAttr [ attr.``class`` "well" ]
////                    [ divAttr [ attr.``class`` "form-group" ]
////                              [ label [ text "Title" ]
////                                Doc.Input [ attr.``class`` "form-control" ] title ]
////                    
////                      divAttr [ attr.``class`` "form-group" ] 
////                              [ label [ text "Elements" ]
////                                
////                                Doc.Button "-" [ attr.``class`` "btn btn-default" ] (fun () -> if rv.Pages.Value.Length >= 0 then Var.Set rv.Pages (rv.Pages.Value |> List.take (rv.Pages.Value.Length - 1)))
////                                Doc.Button "+" [ attr.``class`` "btn btn-default" ] (fun () -> Var.Set rv.Pages (rv.Pages.Value @ [ { Number = Var.Create 0; Content = Var.Create "" } ])) 
////                                
////                                rv.Pages.View
////                                |> Doc.BindView (List.map ReactivePage.RenderBuilder >> Doc.Concat) ] ]
//
//[<JavaScript>]
//module Client =
//    open Model
//    open Builder
//
//    let main() =
//        let title = 
//            Var.Create "New book"
//
//        let pages =
//            ListModel.Create (fun p -> p.Number) []
//
//        pages.len
//
//        let rvBook = { Title = title.View
//                       Pages = pages.View }
//        
//        let container content =
//            divAttr [ attr.style "position:fixed; height: 85%; width: 48%; top: 10%; overflow-y: scroll;"
//                      attr.``class`` "well" ]
//                    [ content ]
//
//        divAttr [ attr.``class`` "container-fluid" ]
//                [ h1Attr [ attr.``class`` "text-center" ] 
//                         [ text "Book - Live preview" ]
//                  divAttr [ attr.``class`` "row" ]
//                          [ divAttr [ attr.``class`` "col-xs-6" ]
//                                    [ divAttr [ attr.``class`` "well" ]
//                                              [ divAttr [ attr.``class`` "form-group" ]
//                                                        [ label [ text "Title" ]
//                                                          Doc.Input [ attr.``class`` "form-control" ] title ]
//                                                divAttr [ attr.``class`` "form-group" ] 
//                                                        [ label [ text "Elements" ]
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
//                               
////                                                          pages.View
////                                                          |> Doc.BindView (List.map ReactivePage.RenderBuilder >> Doc.Concat)
//                                                          ] ] ]
//                            divAttr [ attr.``class`` "col-xs-6" ]
//                                    [ rvBook
//                                      |> ReactiveBook.View
//                                      |> Doc.BindView Book.Render
//                                      |> container ] ] ]
//
//module Server =
//
//    type Page = { Body: Doc list }
//
//    let template =
//        Content.Template<Page>(__SOURCE_DIRECTORY__ + "/index.html")
//            .With("body", fun x -> x.Body)
//    
//    let site =
//        Application.SinglePage (fun _ ->
//            Content.WithTemplate template
//                { Body = [ client <@ Client.main() @> ] })
//
//do Warp.RunAndWaitForInput Server.site |> ignore