module Program =

    open BenchmarkDotNet.Attributes 
    open BenchmarkDotNet.Running
    open System
    open System.Reflection
    open System.IO
    open System.Collections.Concurrent

    let getStrings len = Array.init len (fun _ -> Path.GetRandomFileName())

    let lookup arr (dict:ConcurrentDictionary<string,bool>) =
        arr |> Array.iteri(fun idx elm -> 
            let mutable b = dict.[elm]
            b <- dict.[arr.[0]])


    type StringKeyComparison () =
        let mutable arr : string [] = [||]
        let dict1 = ConcurrentDictionary<_,_>()
        let dict2 = ConcurrentDictionary<_,_>(StringComparer.Ordinal)

        [<Params (100, 500, 1000, 2000)>] 
        member val public DictSize = 0 with get, set

        [<Setup>]
        member self.SetupData() =
            dict1.Clear(); dict2.Clear()
            arr <- getStrings self.DictSize
            arr |> Array.iter (fun x -> dict1.[x] <- true ; dict2.[x] <- true)

        [<Benchmark>]
        member self.StandardLookup () = lookup arr dict1

        [<Benchmark>]
        member self.OrdinalLookup () = lookup arr dict2


    let defaultSwitch () =
        Assembly.GetExecutingAssembly().GetTypes() |> Array.filter (fun t ->
            t.GetMethods ()|> Array.exists (fun m -> 
                m.GetCustomAttributes (typeof<BenchmarkAttribute>, false) <> [||] ))
        |> BenchmarkSwitcher

    let [<EntryPoint>] main args =
        defaultSwitch().Run args 
        0