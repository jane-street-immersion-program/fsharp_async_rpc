module Async_rpc.Server

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open Core_kernel
open Bin_prot

type t = { port : int }

val create_without_port :
  IPAddress ->
  Time_source.t ->
  unit Implementation.t list ->
  (Or_error.t<Connection.t> -> unit) ->
    t

val create :
  int ->
  IPAddress ->
  Time_source.t ->
  unit Implementation.t list ->
  (Or_error.t<Connection.t> -> unit) ->
    t
