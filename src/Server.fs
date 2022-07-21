module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot

//will the implementation_f's, the rpc_name's, and the rpc_versions, be lists since the server can support multiple rpcs, but all of them would share the same reader and writer
let writeToSocket time implementation_list (socket: Socket) =
    let stream = new NetworkStream(socket) :> System.IO.Stream
    Async_rpc.Connection.create
    //steam is the only clear difference between the client and the server
        stream
        time
        Async_rpc.Known_protocol.Rpc
        {| max_message_size = Async_rpc.Transport.default_max_message_size |}
        (fun connection ->
            match connection with
            | Error error -> failwithf "Connection failed%A" error |> ignore
            | Ok _connections -> ()

            ())
        implementation_list
    ()


let startListening port local time implementation_list=
    //ip port, implementatsion, protocol, time source
//todo support different protocols and implementations
    let listener = new TcpListener(localaddr = local, port = port)
    listener.Start()
    printfn "%i is the port" port
    async {
        while true do
            Console.WriteLine "Waiting for connection..."
            listener.Server.Accept() |> (writeToSocket time implementation_list)
    }
    
