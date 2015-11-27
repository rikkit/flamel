//
// Functions related to watching files for changes.
//
// Author:
//       ipavl <ipavl@users.sourceforge.net>
//

module FileWatcher
    open System
    open System.IO

    /// Sets up a FileSystemWatcher object to monitor a directory for changes.
    let setupFileWatcher (path :string, ext :string) =    
        let watcher = new FileSystemWatcher()

        watcher.Path <- path
        watcher.Filter <- "*." + ext
        watcher.EnableRaisingEvents <- true
        watcher.IncludeSubdirectories <- true

        watcher.Changed.Add(fun _ -> Parser.parseFolder(path))
        watcher.Created.Add(fun _ -> Parser.parseFolder(path))
        watcher.Deleted.Add(fun _ -> Parser.parseFolder(path))
        watcher.Renamed.Add(fun _ -> Parser.parseFolder(path))