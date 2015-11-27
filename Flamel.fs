//
// The main source file for Flamel. Handles running the application and setting up services.
//
// Author:
//       ipavl <ipavl@users.sourceforge.net>
//

open System
open System.Text
open System.IO

[<EntryPoint>]
let main argv = 
    // Parse args to get the source directory if applicable, otherwise use the current directory
    let src = match argv with
        | [|dir|] -> dir
        | _ -> Environment.CurrentDirectory

    printfn "Flamel static site generator v0.4"
    printfn "Using source directory: %s" (src.ToString())

    // Do an initial parse
    Parser.parseFolder(src.ToString())

    // Launch a web server to serve the files
    WebServer.listener (fun req resp ->
    async {
        let data = Encoding.ASCII.GetBytes(WebServer.routeHandler (req, src.ToString()))
        resp.OutputStream.Write(data, 0, data.Length)
        resp.OutputStream.Close()
    })
    printfn "Started server at %s" WebServer.listenAddress

    // Set up the file watcher to reparse the source files on changes    
    for ext in Parser.markdownFileExts do
        FileWatcher.setupFileWatcher(src, ext)

    FileWatcher.setupFileWatcher(Path.Combine(src, "_includes"), "html")

    // Keeps the application running until the user presses a key to exit
    Console.ReadLine() |> ignore
    printfn "Received a keypress. Exiting..."

    0 // return an integer exit code
