module Async_rpc.Implementation

open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
open Async_rpc

type 'connection_state t =
    { rpc_tag: Rpc_tag.t
      rpc_version: int64
      partial_fun: 'connection_state
          -> Bin_prot.Nat0.t Query.t
          -> buf
          -> pos_ref
          -> Transport.Writer.t
          -> unit Transport.Send_result.t
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
    (*Extracts a 'query from the binary message using buffers and stuff to read it in*)
    let query_ =
        let len = query.data
        (*Binary info turned into info f sharp can use*)
        let data =
            Bin_prot_reader.read_and_verify_length
                query_reader
                None
                read_buffer
                read_buffer_pos_ref
                len
                "client-side rpc response un-bin-io'ing"
        match data with
        | Ok data -> data
        | Error error -> failwithf "client-side rpc response un-bin-io'ing %A" error
//question: starting here a new thread should be made i think but it also needs to able to be able to write into the bin_prot  
    let resp =  f connection_state query_
    let result: _ Response.t = { id = query.id; data = Ok resp } in
    (*'response Bin_prot.Type_class.writer turns the response we get into the "binary blob" that will go back into the binary stream that will go to the client*)
    //should return a handler_result async so that it can be an error and also so that this section can run on a seperate thread
    let result =
        Transport.Writer.send_bin_prot
            transport_writer
            (Message.bin_writer_needs_length (Writer_with_length.of_writer response_writer))
            (Message.t.Response result)

    result
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
