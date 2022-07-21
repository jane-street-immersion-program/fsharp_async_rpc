module Async_rpc.Implementation
open Core_kernel
open Async_rpc.Protocol
open Bin_prot.Common
(* CR hleung for ashah: what to do with result_mode?? *)
type 'connection_state t
(*Response may change to result*)
// todo: make sure these types are correct, especially the apply
val create:
    ('query Bin_prot.Type_class.reader) ->
    ('connection_state -> 'query -> 'response) ->
    ('response Bin_prot.Type_class.writer) ->
    Rpc_tag.t ->
    int64 ->
        'connection_state t

val apply:
    'connection_state t ->
    'connection_state ->
    Bin_prot.Nat0.t Query.t ->
    buf ->
    pos_ref ->
    Transport.Writer.t ->
        Result.t<unit, Rpc_error.t>

val rpc_info: (_ t) -> Rpc_tag.t * int64
