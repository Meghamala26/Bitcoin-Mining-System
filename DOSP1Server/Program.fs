/// Code for Bitcoin Mining Server

#if INTERACTIVE
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote" 
#r "nuget: Akka.TestKit"
#endif

open System
open System.Text
open Akka.Actor
open Akka.Configuration
open Akka.Routing
open Akka.FSharp
open Akka.Remote
open System.Security.Cryptography
open System.Threading
open System.Diagnostics


/// configuring the Server system

let configuration =
    Configuration.parse
        @"akka {
            log-config-on-start = on
            stdout-loglevel = DEBUG
            loglevel = DEBUG
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote {
                helios.tcp {
                    transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                    applied-adapters = []
                    transport-protocol = tcp
                    port = 8080
                    hostname = localhost
                }
            }
        }"


let system=System.create "MiningServer" configuration

let numberOfWorkers = System.Environment.ProcessorCount

let printBitcoin coin =
    printfn "%s" coin

let CoinBucket (f : 'a -> unit) = actorOf f

let printBitcoinRef =
    CoinBucket printBitcoin
    |> spawn system "CoinBucketSystem"


/// function to create a random string for nonce

let randomCreateString len = 
    let rand = Random()
    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let sz = Array.length chars in
    String(Array.init len (fun _ -> chars.[rand.Next sz]))


/// function to convert byte to hex

let ByteHexConv bytes = 
    bytes 
    |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x))
    |> String.concat System.String.Empty
    

/// function to generate hash code with SHA256

let generateHash inputString =    
    let hashedInput = 
        string(inputString) 
        |>Encoding.UTF8.GetBytes 
        |>(new SHA256Managed()).ComputeHash 
        |>ByteHexConv    
    hashedInput

/// function to find legit Bitcoins with given number of leading zeros 

let findCoin numberOfLeadingZeros =   
    let mutable zeroString=""
    let mutable coin=""
    let prefix="m.gupta1"
    for i=1 to numberOfLeadingZeros do
        zeroString<-zeroString+ "0"    
    while (true) do
        let nonce=randomCreateString 20
        let inputString = prefix + nonce
        let hashedString= generateHash inputString
        let n=numberOfLeadingZeros-1
        if (hashedString. [..n]).Equals(zeroString) then
            coin<-inputString + " " + hashedString
            printBitcoinRef <! coin


/// Server worker 

let worker (mailbox: Actor<'a>)= 
    let rec  loop () =
        actor {
            let! msg = mailbox.Receive()            
            findCoin msg
            return! loop()
        }
    loop()

let workerRef =
    worker
    |> spawnOpt system "Worker" 
    <| [SpawnOption.Router(RoundRobinPool(numberOfWorkers))]


/// master

let master (numberOfWorkers: int) (numberOfZeros:int) (mailbox: Actor<'a>)=                 
    let rec  loop () =
        actor {
            let! msg = mailbox.Receive()
            let sender =mailbox.Sender()
            printfn "%A" msg
            match box msg with
            | :? string as msg when msg="Assign" -> 
                printfn "Client requesting work."
                sender <! "m.gupta1" + " " + string numberOfZeros
            | :? string as msg when msg="Ack" -> 
                printfn "Work acknowledged by Client."
            | _ -> 
                printfn "Running Self."
                for i=1 to numberOfWorkers do
                    workerRef <! numberOfZeros
            return! loop()
        }
    loop()


[<EntryPoint>]
let main argv =    
    let numberOfLeadingZeros = int argv.[0]
    let proc = Process.GetCurrentProcess()
    let cpuTimeStamp = proc.TotalProcessorTime
    let timer = new Stopwatch()
    let masterRef =
        master numberOfWorkers numberOfLeadingZeros
        |> spawn system "masteractor"
    printfn "Number of leading zeros: %i" numberOfLeadingZeros
    printfn "Number of workers: %i" numberOfWorkers
    timer.Start()
    try
        masterRef <! "Self"
        Thread.Sleep(120000)
    finally
        let cpuTime = (proc.TotalProcessorTime - cpuTimeStamp).TotalMilliseconds
        printfn "CPU time = %d ms" (int64 cpuTime)
        printfn "Elapsed time = %d ms" timer.ElapsedMilliseconds
        printfn "Ratio = %f" (cpuTime / float(timer.ElapsedMilliseconds))
    0 // return an integer exit code