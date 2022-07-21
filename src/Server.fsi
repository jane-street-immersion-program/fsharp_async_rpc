module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot
val writeToSocket: Time_source.t ->unit Implementation.t list->Socket ->unit

val startListening: int -> IPAddress-> Time_source.t -> unit Implementation.t list-> Async<unit>
