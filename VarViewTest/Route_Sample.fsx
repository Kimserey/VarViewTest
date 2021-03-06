﻿#I "../packages/"
#load "WebSharper.Warp/tools/reference-nover.fsx"

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

[<JavaScript>]
module Client =
    
    type DesignerEndpoint =
        | Default
        | Form     of formId: string
        | Sections of formId: string
        | Section  of formId: string * sectionId: string
    
    let main() =
        let map =
            RouteMap.Create 
                (function
                 | Form formId                 -> JS.Alert "endpoint -> string []"; [ "forms"; formId ]
                 | Sections formId             -> [ "forms"; formId; "sections" ]
                 | Section (formId, sectionId) -> [ "forms"; formId; "sections"; sectionId ]
                 | Default                     -> [ "" ]) 
                (function
                 | [ "forms"; formId ]                        -> JS.Alert "string [] -> endpoint"; Form formId
                 | [ "forms"; formId; "sections" ]            -> Sections formId
                 | [ "forms"; formId; "sections"; sectionId ] -> Section (formId, sectionId)
                 | _                                          -> Default)
        
        let route =
            RouteMap.Install map

        [ Doc.Button "Go Hello" [] (fun () -> Var.Set route (Form "hello")) :> Doc
          Doc.Button "Go World" [] (fun () -> Var.Set route (Section ("hello", "world"))) :> Doc
          
          route.View
          |> Doc.BindView (function 
                           | Form formId                 -> Doc.Empty
                           | Sections formId             -> Doc.Empty
                           | Section (formId, sectionId) -> Doc.Empty
                           | Default                     -> Doc.Empty) ]
        |> Doc.Concat


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