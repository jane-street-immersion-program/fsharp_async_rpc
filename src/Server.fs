module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot
open System.Threading

type t = { port: int }

//will the implementation_f's, the rpc_name's, and the rpc_versions, be lists since the server can support multiple rpcs, but all of them would share the same reader and writer
let writeToSocket time implementation_list connection_callback (socket: Socket) =
    let stream = new NetworkStream(socket) :> System.IO.Stream

    Async_rpc.Connection.create
        //steam is the only clear difference between the client and the server
        stream
        time
        Async_rpc.Known_protocol.Rpc
        {| max_message_size = Async_rpc.Transport.default_max_message_size |}
        connection_callback
        implementation_list

    ()

let create_without_port
    local
    (time: Time_source.t)
    (implementation_list: unit Implementation.t list)
    (connection_callback: Connection.t Or_error.t -> unit)
    =
    //ip port, implementatsion, protocol, time source
//todo support different protocols and implementations
    //ip port, implementatsion, protocol, time source
//todo support different protocols and implementations
    let listener = new TcpListener(local, 0)
    listener.Start()
    let port = (listener.LocalEndpoint :?> IPEndPoint).Port
    let t = { port = port }
    printfn "%A" SynchronizationContext.Current

    async {
        printfn ("looping task")

        while true do
            Console.WriteLine "Waiting for connection..."

            task {
                listener.Server.Accept()
                |> (writeToSocket time implementation_list connection_callback)
            }
            |> ignore

            ()
    }
    |> Async.Start

    t

let create port local time implementation_list connection_callback =
    //ip port, implementatsion, protocol, time source
//todo support different protocols and implementations
    let listener = new TcpListener(localaddr = local, port = port)
    listener.Start()
    printfn "%i is the port" port
    let t = { port = port }

    async {
        while true do
            Console.WriteLine "Waiting for connection..."

            task {
                listener.Server.Accept()
                |> (writeToSocket time implementation_list connection_callback)
            }
            |> ignore
    }
    |> Async.Start

    t
