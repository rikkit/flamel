﻿//
// The main source file for Flamel.
//
// Author:
//       ipavl <ipavl@users.sourceforge.net>
//

open FSharp.Markdown

open System
open System.IO
open System.Text
open System.Collections.Generic

/// Functions that fetch files that should be included.
module Include =
    /// Reads the header include file.
    let header(dir : string) =
        File.ReadAllText (dir + "/_includes/header.inc.html")

    /// Reads the body include file.
    let body(dir : string) =
        File.ReadAllText (dir + "/_includes/body.inc.html")

    /// Reads the navigation include file.
    let navigation(dir : string) =
        File.ReadAllText (dir + "/_includes/nav.inc.html")

    /// Reads the footer include file.
    let footer(dir : string) =
        File.ReadAllText (dir + "/_includes/footer.inc.html")

/// Functions that parse specific metadata elements, such as the page title and date.
module Metadata =
    /// Extracts metadata such as the date and title from the passed Markdown.
    let extract(markdown : string) =
        let dict = new Dictionary<string, string>();
            
        // Active pattern to match metadata prefixes
        let (|Prefix|_|) (p:string) (s:string) =
            if s.StartsWith(p) then
                Some(s.Substring(p.Length))
            else
                None

        // TODO: Make this only loop for the metadata section as this parses metadata in actual content
        for line in markdown.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries) do
            match line with
            | Prefix "title: " rest -> dict.Add("title", rest)
            | Prefix "date: " rest -> dict.Add("date", rest)
            | _ -> ()

        dict

/// Functions that are used to parse files (e.g. Markdown, templates).
module Parse =
    /// Parses and converts Markdown files into HTML files.
    let markdown(dir : string) =
        let timer = Diagnostics.Stopwatch.StartNew()

        for mdFile in Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories) do
            let htmlFile = Path.ChangeExtension(mdFile, "html")
            let mdArray = File.ReadAllLines mdFile

            // convert the array to a multiline string
            let lines =
                let re = Text.RegularExpressions.Regex(@"#(\d+)")
                [|for line in mdArray ->
                    re.Replace(line.Replace("{", "{{").Replace("}", "}}").Trim(), "$1", 1)|]
            let mdString = String.Join("\n", lines)

            let metadata = Metadata.extract(mdString)   // extract the metadata into a dictionary

            // rebuild the Markdown string without the metadata block to parse for the page content
            let markdown =
                let sb = new Text.StringBuilder()

                // metadata.Count is the number of items we read, and there are two separator lines
                for i in metadata.Count + 2 .. mdArray.Length - 1 do
                    sb.Append(Array.get mdArray i).Append("\n") |> ignore
                sb.ToString()

            let html = Markdown.TransformHtml(markdown)
            let page : string =
                Include.header(dir)
                + metadata.Item("title")
                + Include.body(dir)
                + Include.navigation(dir)
                + html
                + Include.footer(dir)

            File.WriteAllText(htmlFile, page)

            printfn "%s -> %s" mdFile htmlFile

        timer.Stop()
        printfn "Done in %f ms" timer.Elapsed.TotalMilliseconds

[<EntryPoint>]
let main argv = 
    let src = new Text.StringBuilder()

    // parse args
    match argv with
    | [|dir|] -> src.Append(Environment.CurrentDirectory).Append("/").Append(dir)
    | _ -> src.Append(Environment.CurrentDirectory)
    |> ignore

    printfn "Flamel static site generator v0.3"
    printfn "Using source directory: %s" (src.ToString())

    // Launch a web server to serve the files
    WebServer.listener (fun req resp ->
    async {
        let data = Encoding.ASCII.GetBytes(WebServer.routeHandler (req, src.ToString()))
        resp.OutputStream.Write(data, 0, data.Length)
        resp.OutputStream.Close()
    })
    printfn "Started server at %s" WebServer.httpServer

    Parse.markdown(src.ToString())

    Console.ReadLine() |> ignore

    0 // return an integer exit code
