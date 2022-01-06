/// Code for Bitcoin Mining Client

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


/// configuring the Client system

let configuration =
    Configuration.parse
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote {
                helios.tcp {
                    transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
		            applied-adapters = []
		            transport-protocol = tcp
		            port = 0
		            hostname = localhost
                }
            }
        }"

let system = System.create "MiningClient" configuration


/// defining the master

type ClientMessage = Tuple of string * int

let master ipAddress workerRef (mailbox: Actor<'a>) =
    let url = "akka.tcp://MiningServer@" + ipAddress + ":8080/user/masteractor"
    let serverMaster = select url system
    let rec loop() = actor {
        let! message = mailbox.Receive()
        let sender = mailbox.Sender()
        match box message with
        | :? string as msg when (msg |> String.exists (fun char -> char = ' ')) ->
                printfn "Work has been assigned. Mining started."
                sender <!"Ack"
                let result = msg.Split ' '
                let inputString = result.[0]
                let numberOfLeadingZeros = int result.[1]
                for i=1 to System.Environment.ProcessorCount do
                    workerRef <! Tuple(inputString, numberOfLeadingZeros)
        | _ ->
                printfn "Requesting work from Server."
                serverMaster <! "Assign"
        return! loop()
    }
    loop()

/// function to create a random string for nonce

let randomCreateString len = 
    let rand = Random()
    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let rndStr = Array.length chars in
    String(Array.init len (fun _ -> chars.[rand.Next rndStr]))


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

let findCoin prefix numberOfLeadingZeros printBitcoinRef =   
    let mutable zeroString=""
    let mutable coin=""
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


// Client worker sending legit Bitcoins found to Server's coin bucket

let worker ipAddress (mailbox: Actor<'a>)= 
    let url = "akka.tcp://MiningServer@" + ipAddress + ":8080/user/CoinBucketSystem"
    let printBitcoinRef = select url system
    let rec  loop () =
        actor {
            let! Tuple(inputString, numberOfLeadingZeros) = mailbox.Receive()
            findCoin inputString numberOfLeadingZeros printBitcoinRef
            return! loop()
        }
    loop()


[<EntryPoint>]
let main argv =
    let ipAddress = string argv.[0]
    printfn "Server IP Addresses: %s" ipAddress
    let numberOfWorkers = System.Environment.ProcessorCount
    let workerRef =
        worker ipAddress
            |> spawnOpt system "MinerWorker" 
            <| [SpawnOption.Router(RoundRobinPool(numberOfWorkers))]
    let masterRef =
        master ipAddress workerRef
        |> spawn system "master"
    masterRef <! "AskForWork"
    Thread.Sleep(6000000)
    0 // return an integer exit code