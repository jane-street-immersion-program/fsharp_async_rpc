module Async_rpc.Implementation

open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
open Async_rpc
open Transport

type 'connection_state t =
    { rpc_tag: Rpc_tag.t
      rpc_version: int64
      partial_fun: 'connection_state
          -> Bin_prot.Nat0.t Query.t
          -> buf
          -> pos_ref
          -> Transport.Writer.t
          -> Result.t<unit, Rpc_error.t>
<<<<<<< HEAD
=======
    (*; implement: ('connection_state -> Buffer.t -> Buffer.t)*)
>>>>>>> ec23e7a927aa331442e8fa83058b083345c54c1d
     }

let real_function
    (query_reader: 'query Bin_prot.Type_class.reader)
    (f: 'connection_state -> 'query -> 'response)
    response_writer
    connection_state
    (query: Bin_prot.Nat0.t Query.t)
    read_buffer
    read_buffer_pos_ref
    transport_writer
    =
    Result.let_syntax {
        let! query_ =
            let len = query.data
<<<<<<< HEAD
=======
            (*Binary info turned into info f sharp can use*)
>>>>>>> ec23e7a927aa331442e8fa83058b083345c54c1d
            Bin_prot_reader.read_and_verify_length
                query_reader
                None
                read_buffer
                read_buffer_pos_ref
                len
                "client-side rpc response un-bin-io'ing"
<<<<<<< HEAD

=======
>>>>>>> ec23e7a927aa331442e8fa83058b083345c54c1d

        task {
            let (resp: Or_error.t<'response>) =
                Core_kernel.Or_error.try_with (fun () -> f connection_state query_)

            match resp with
            | Ok resp ->
                let result: _ Response.t = { id = query.id; data = Ok resp } in
                let result =
                    Transport.Writer.send_bin_prot
                        transport_writer
                        (Message.bin_writer_needs_length (Writer_with_length.of_writer response_writer))
                        (Message.t.Response result)
                    |> Transport.Send_result.to_or_error

                match result with
                | Ok () -> ()
                | Error _error ->
                    Writer.close transport_writer |> ignore
            | Error _error ->
                return ()
        }
        |> ignore

        return ()
    }
<<<<<<< HEAD
=======

(*This will return the function that needs the remaining arguments to complete real_function, if we call this in connection anyway how are we any better off since the function returns the function needing more arguments in connection*)
>>>>>>> ec23e7a927aa331442e8fa83058b083345c54c1d
let apply t connection_state (query: Bin_prot.Nat0.t Query.t) read_buffer read_buffer_pos_ref transport =
    t.partial_fun connection_state query read_buffer read_buffer_pos_ref transport

let create query_reader f response_writer rpc_name rpc_version =
    let t =
        { rpc_tag = rpc_name
          rpc_version = rpc_version
          partial_fun = (real_function query_reader f response_writer) }

    t

let rpc_info (implementation: _ t) =
    (implementation.rpc_tag, implementation.rpc_version)
