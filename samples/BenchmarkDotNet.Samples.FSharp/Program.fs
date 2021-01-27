module Program

open System
open System.IO
open System.Collections.Concurrent
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

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

    [<GlobalSetup>]
    member self.GlobalSetupData() =
        dict1.Clear(); dict2.Clear()
        arr <- getStrings self.DictSize
        arr |> Array.iter (fun x -> dict1.[x] <- true ; dict2.[x] <- true)

    [<Benchmark>]
    member self.StandardLookup () = lookup arr dict1

    [<Benchmark>]
    member self.OrdinalLookup () = lookup arr dict2

#if NETFRAMEWORK
[<BenchmarkDotNet.Diagnostics.Windows.Configs.TailCallDiagnoser>]
#endif
type TailCallDetector () =
    
    let rec factorial n =
            match n with
            | 0 | 1 -> 1
            | _ -> n * factorial(n-1)
            
    let factorial1 n =
        let rec loop i acc =
            match i with
            | 0 | 1 -> acc
            | _ -> loop (i-1) (acc * i)
        loop n 1
        
    let factorial2 n =
        let rec tailCall n f =
            if n <= 1 then
                f()
            else
                tailCall (n - 1) (fun () -> n * f())
 
        tailCall n (fun () -> 1)

    [<Params (7)>] 
    member val public facRank = 0 with get, set
            
    [<Benchmark>]
    member self.test () =
       factorial self.facRank
    
    [<Benchmark>]
    member self.test1 () =
       factorial1 self.facRank
    
    [<Benchmark>]
    member self.test2 () =
       factorial2 self.facRank
       
let defaultSwitch () = BenchmarkSwitcher [|typeof<StringKeyComparison>; typeof<TailCallDetector>|]

[<EntryPoint>]
let Main args =
    let summary = defaultSwitch().Run args
    0
