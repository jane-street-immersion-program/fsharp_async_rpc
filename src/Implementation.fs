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
    (*; implement: ('connection_state -> Buffer.t -> Buffer.t)*)
     }

(*val rpc :'query Bin_prot.Type_class.reader -> 'response Bin_prot.Type_class.writer -> ('connection_state -> 'query -> 'response) -> 'connection_state t*)
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
        (*Extracts a 'query from the binary message using buffers and stuff to read it in*)
        let! query_ =
            let len = query.data
            (*Binary info turned into info f sharp can use*)
            Bin_prot_reader.read_and_verify_length
                query_reader
                None
                read_buffer
                read_buffer_pos_ref
                len
                "client-side rpc response un-bin-io'ing"


        //threading starts here, this will allow a thread to run f seperately and if an error gets thrown close the thread this f is run on
        //where we use try response thingy to see if f fails
        task {
            let (resp: Or_error.t<'response>) =
                Core_kernel.Or_error.try_with (fun () -> f connection_state query_)

            match resp with
            | Ok resp ->
                //never gets here when infinite loop
                printf ("passed loop")
                let result: _ Response.t = { id = query.id; data = Ok resp } in
                //should return a handler_result async so that it can be an error and also so that this section can run on a seperate thread
                //question:why is this a unit?
                let result =
                    //if this fails then we can close the Transport writer  all together
                    Transport.Writer.send_bin_prot
                        transport_writer
                        (Message.bin_writer_needs_length (Writer_with_length.of_writer response_writer))
                        (Message.t.Response result)
                    |> Transport.Send_result.to_or_error

                match result with
                | Ok () -> ()
                | Error error ->
                    printfn "Transport Writer sending error: %A" error
                    Writer.close transport_writer |> ignore
            //sprintf "%A" error |> Sexp.t.Atom|> Protocol.Rpc_error.t.Write_error |> Or_error.Error.format "%A"
            //why does the compiler not recognize to return this
            | Error error ->
                printfn "%A" error
                return ()
        }
        |> ignore

        return ()
    }

(*This will return the function that needs the remaining arguments to complete real_function, if we call this in connection anyway how are we any better off since the function returns the function needing more arguments in connection*)
let apply t connection_state (query: Bin_prot.Nat0.t Query.t) read_buffer read_buffer_pos_ref transport =
    t.partial_fun connection_state query read_buffer read_buffer_pos_ref transport

let create query_reader f response_writer rpc_name rpc_version =
    //creates implementatisons.t
    let t =
        { rpc_tag = rpc_name
          rpc_version = rpc_version
          partial_fun = (real_function query_reader f response_writer) }

    t

let rpc_info (implementation: _ t) =
    (implementation.rpc_tag, implementation.rpc_version)
