module Async_rpc.Test.Server_test

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Bin_prot
open Core_kernel
open Async_rpc
open Async_rpc.Protocol
open System.Threading.Tasks
open System.Runtime.CompilerServices
open NUnit.Framework
open System.Threading

let start_client_with_implementations port implementation_list =
    let dispatch_tcs = new TaskCompletionSource<Or_error.t<string>>()
    let client = new TcpClient("127.0.0.1", port)
    let stream = client.GetStream() :> System.IO.Stream
    let time = new Time_source.Wall_clock.t ()
    let wait = new Auto_reset_event.t false

    let connection_callback =
        (fun connection ->
            match connection with
            | Error _error -> failwith "Connection failed"
            | Ok connections ->
                let Rpc =
                    Async_rpc.Rpc.create {| name = "Rpc"; version = 9 |} Type_class.bin_string Type_class.bin_string

                match
                    //we set tcs in the callback not in whether or not the dispatch suceeded
                    Async_rpc.Rpc.dispatch Rpc connections "Hello" (fun result ->
                        match result with
                        | Error error -> dispatch_tcs.SetResult(Error(Protocol.Rpc_error.to_error (error)))
                        | Ok ok -> dispatch_tcs.SetResult(Ok ok)

                        Auto_reset_event.set wait)
                    with
                | Error _error -> failwith "Dispatch failed"
                | Ok () -> ()

            ())

    task {
        Async_rpc.Connection.create
            stream
            time
            Async_rpc.Known_protocol.Rpc
            {| max_message_size = Async_rpc.Transport.default_max_message_size |}
            connection_callback
            implementation_list
    }
    |> ignore

    dispatch_tcs

let start_client port =
    let dispatch_tcs = new TaskCompletionSource<Or_error.t<unit>>()
    let client = new TcpClient("127.0.0.1", port)

    let stream = client.GetStream() :> System.IO.Stream
    let time = new Time_source.Wall_clock.t ()
    let wait = new Auto_reset_event.t false

    let connection_callback =
        (fun connection ->
            match connection with
            | Error error -> dispatch_tcs.SetResult(Error error)
            | Ok connections ->
                let Rpc =
                    Async_rpc.Rpc.create {| name = "Rpc"; version = 9 |} Type_class.bin_string Type_class.bin_string

                match
                    Async_rpc.Rpc.dispatch Rpc connections "hi from the client" (fun result ->
                        printfn "%A" result
                        Auto_reset_event.set wait)
                    with
                | Error error -> dispatch_tcs.SetResult(Error error)
                | Ok () -> dispatch_tcs.SetResult(Ok())

            ())

    let implementation_f = (fun _foo _bar -> ())

    task {
        Async_rpc.Connection.create
            stream
            time
            Async_rpc.Known_protocol.Rpc
            {| max_message_size = Async_rpc.Transport.default_max_message_size |}
            connection_callback
            [ Async_rpc.Implementation.create
                  Type_class.bin_unit.reader
                  implementation_f
                  Type_class.bin_unit.writer
                  "Rpc"
                  9 ]
    }
    |> ignore

    dispatch_tcs

let start_server () =
    let time = new Time_source.Wall_clock.t ()
    //todo is this where we can make a while true loop to test multi-threading with clients?
    let implementation_fun =
        (fun _foo _bar ->
            printf "a"

            while true do
                ())

    let local_ip = IPAddress.Parse "127.0.0.1"
    let rpc_name = "Rpc"
    let rpc_version = 9

    let implementation_list =
        [ Async_rpc.Implementation.create
              Type_class.bin_string.reader
              implementation_fun
              Type_class.bin_unit.writer
              rpc_name
              rpc_version ]

    let connection_callback =
        (fun connection ->
            match connection with
            | Error error -> failwithf "Connection failed%A" error |> ignore
            | Ok _connections -> ())

    Async_rpc.Server.create_without_port local_ip time implementation_list connection_callback


let start_server_with_implementations implementation_list =
    let time = new Time_source.Wall_clock.t ()

    let local_ip = IPAddress.Parse "127.0.0.1"

    let connection_callback =
        (fun connection ->
            match connection with
            | Error error -> failwithf "Connection failed%A" error |> ignore
            | Ok _connections -> ())

    Async_rpc.Server.create_without_port local_ip time implementation_list connection_callback

[<Test>]
let ``Test Server`` () =

    let server = start_server ()
    let client1_tcs = start_client server.port
    let client2_tcs = start_client server.port

    match client1_tcs.Task.Result with
    | Error error -> failwithf "%A" error
    | Ok _ok -> ()

    match client2_tcs.Task.Result with
    | Error error -> failwithf "%A" error
    | Ok _ok -> ()

    ()

[<Test>]
let ``Test query`` () =
    let implementation_f = (fun _foo bar -> bar + " hi")
    let implementation_list =
        [ Async_rpc.Implementation.create
              Type_class.bin_string.reader
              implementation_f
              Type_class.bin_string.writer
              "Rpc"
              9 ]

    let server = start_server_with_implementations implementation_list
    let client1_tcs = start_client_with_implementations server.port implementation_list

    match client1_tcs.Task.Result with
    | Error error -> failwithf "%A" error
    | Ok ok ->
        match ok with
        | "Hello hi" -> printf ("pASSEDDDDD")
        | _ -> failwith "did not append"

    ()
