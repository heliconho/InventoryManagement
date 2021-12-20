namespace IM.Handler

open Giraffe
open Microsoft.AspNetCore.Http
open System.Text.Json
open IM.Db.UserRepo
open IM.Db.InventoryRepo
open IM.Model

module UserHanlder = 
    let loginHandler = 
        fun (next :HttpFunc) (ctx: HttpContext) ->
            task {
                let! loginpayload = JsonSerializer.DeserializeAsync<LoginRequest>(ctx.Request.Body)
                let! loginResult = login loginpayload
                match loginResult with
                | Ok res -> 
                    return! json res next ctx
                | Error res -> 
                    return! text res.msg next ctx 
            }

    let registerHandler = 
        fun (next :HttpFunc) (ctx :HttpContext) ->
            task {
                let! registerpayload = JsonSerializer.DeserializeAsync<RegisterRequest>(ctx.Request.Body)
                let! registerResult = register registerpayload
                match registerResult with
                | Ok res -> 
                    return! json res next ctx
                | Error res -> 
                    return! json res next ctx 
            }

module InventoryHandler = 
    open System.Security.Claims
    let createHandler = 
        fun (next :HttpFunc) (ctx :HttpContext) ->
            task{
                let email = ctx.User.FindFirst ClaimTypes.NameIdentifier
                let! insertpayload = JsonSerializer.DeserializeAsync<InventoryRequest>(ctx.Request.Body)
                let! insertResult = insertInventory insertpayload email.Value
                return! json insertResult next ctx
            }
    let createMultipleHandler = 
        fun (next :HttpFunc) (ctx :HttpContext) ->
            task{
                let email = ctx.User.FindFirst ClaimTypes.NameIdentifier
                let! insertpayload = JsonSerializer.DeserializeAsync<InventoriesRequest>(ctx.Request.Body)
                let! insertResult = insertNInventory insertpayload email.Value
                return! json insertResult next ctx
            }
    let getHandler = 
        fun (next :HttpFunc) (ctx: HttpContext) ->
            task{
                let email = ctx.User.FindFirst ClaimTypes.NameIdentifier
                let! getAllResult = getAllInventory email.Value
                return! json getAllResult next ctx
            }