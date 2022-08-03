module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot
open System.Threading

type t = { port : int }


let write_to_socket time implementation_list connection_callback (socket : Socket) =
  let stream = new NetworkStream(socket) :> System.IO.Stream

  Async_rpc.Connection.create
    stream
    time
    Async_rpc.Known_protocol.Rpc
    {| max_message_size = Async_rpc.Transport.default_max_message_size |}
    connection_callback
    implementation_list

  ()

let wait_for_connection
  (listener : TcpListener)
  time
  (implementation_list : list<Implementation.t<unit>>)
  connection_callback
  =
  async {

    while true do
      async {
        listener.Server.Accept()
        |> (write_to_socket time implementation_list connection_callback)
      }
      |> Async.Start
  }
  |> Async.Start

let create_without_port
  local
  (time : Time_source.t)
  (implementation_list : unit Implementation.t list)
  (connection_callback : Connection.t Or_error.t -> unit)
  =
  let listener = new TcpListener(local, 0)
  listener.Start()
  let port = (listener.LocalEndpoint :?> IPEndPoint).Port
  let t = { port = port }
  wait_for_connection listener time implementation_list connection_callback
  t

let create port local time implementation_list connection_callback =
  let listener = new TcpListener(localaddr = local, port = port)
  listener.Start()
  let t = { port = port }
  wait_for_connection listener time implementation_list connection_callback
  t
