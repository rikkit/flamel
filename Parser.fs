//
// Functions related to parsing files.
//
// Author:
//       ipavl <ipavl@users.sourceforge.net>
//

module Parser
    open System
    open System.IO
    open System.Collections.Generic
    open MarkdownSharp

    [<Literal>]
    let METADATA_PREFIX : string = "="

    let markdownFileExts = ["md"; "markdown"; "mdown"]

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
        
    /// Parses and converts Markdown files into HTML files.
    let parseFolder(dir : string) =
        let timer = Diagnostics.Stopwatch.StartNew()

        let allFiles = markdownFileExts
                    |> Seq.collect (fun a -> Directory.EnumerateFiles(dir, "*." + a, SearchOption.AllDirectories))
        
        let options = new MarkdownOptions (AutoHyperlink = true)
        let markdown = new Markdown(options)

        for mdPath in allFiles do 
            let textLines = File.ReadAllLines mdPath

            let metadataLines = textLines |> Seq.takeWhile(fun line -> line.StartsWith METADATA_PREFIX)
            let metadataString = String.Join(Environment.NewLine, metadataLines)
            let metadata = Metadata.extract(metadataString)

            let mdLines = textLines |> Seq.skipWhile(fun line -> line.StartsWith METADATA_PREFIX)
            let mdDoc = String.Join(Environment.NewLine, mdLines)
                    
            let html =
                Include.header(dir)
                + markdown.Transform(mdDoc)
                + Include.footer(dir)
                    
            let htmlPath = Path.ChangeExtension(mdPath, "html") // Store html next to its source file
            File.WriteAllText(htmlPath, html)

        timer.Stop()
        printfn "Done in %f ms" timer.Elapsed.TotalMilliseconds
