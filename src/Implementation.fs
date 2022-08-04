module Async_rpc.Implementation

open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
open Async_rpc
open Transport
open Core_kernel.Bin_prot_generated_types.Lib.Dotnet.Core_with_dotnet.Src

type 'connection_state t =
  { rpc_tag : Rpc_tag.t
    rpc_version : int64
    partial_fun : 'connection_state
      -> Bin_prot.Nat0.t Query.t
      -> buf
      -> pos_ref
      -> Transport.Writer.t
      -> Result.t<unit, Rpc_error.t> }

let implementation_write
  (query_reader : 'query Bin_prot.Type_class.reader)
  (f : 'connection_state -> 'query -> 'response)
  response_writer
  connection_state
  (query : Bin_prot.Nat0.t Query.t)
  read_buffer
  read_buffer_pos_ref
  transport_writer
  =
  Result.let_syntax {
    let! query_ =
      let len = query.data

      Bin_prot_reader.read_and_verify_length
        query_reader
        None
        read_buffer
        read_buffer_pos_ref
        len
        "client-side rpc response un-bin-io'ing"

    async {
      let resp =
        //could the string here be anything and it would work?
        Core_kernel.Or_error.try_with (fun () -> f connection_state query_)
        |> Result.mapError (fun error ->
          Protocol.Rpc_error.t.Uncaught_exn(Sexp.t.Atom(sprintf "%A" error)))

      let result : _ Response.t = { id = query.id; data = resp } in

      let result =
        Transport.Writer.send_bin_prot
          transport_writer
          (Message.bin_writer_needs_length (Writer_with_length.of_writer response_writer))
          (Message.t.Response result)
        |> Transport.Send_result.to_or_error

      match result with
      | Ok () -> printfn "Write successful"
      | Error error ->
        //error only occurs when theres an issue with the connection, in which case the connection will close,
        //this is why we ignore the error values
        printfn ("Write error: %A") error
        Writer.close transport_writer

      return ()
    }
    |> Async.Start

    return ()
  }

let apply
  t
  connection_state
  (query : Bin_prot.Nat0.t Query.t)
  read_buffer
  read_buffer_pos_ref
  transport
  =
  t.partial_fun connection_state query read_buffer read_buffer_pos_ref transport

let create query_reader f response_writer rpc_name rpc_version =
  let t =
    { rpc_tag = rpc_name
      rpc_version = rpc_version
      partial_fun = (implementation_write query_reader f response_writer) }

  t

let rpc_info (implementation : _ t) =
  (implementation.rpc_tag, implementation.rpc_version)
