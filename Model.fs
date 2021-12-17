namespace IM.Model
open MongoDB.Bson

open System

[<CLIMutable>]
type Category = {
    Id : BsonObjectId
    category_id : int
    CatgeoryName : string
}

[<CLIMutable>]
type Company = {
    Id : BsonObjectId
    CompanyName : string
}

[<CLIMutable>]
type InventoryDetail = {
    Id : BsonObjectId
    InventoryName : string
    Description : string
    SKU : string
    Category : string[]
    Quantity : int
    DateCreated: string
    User : BsonObjectId
    DateChecked: string
    Active : bool
}

type InventoryRequest = {
    InventoryName : string
    Description : string
    SKU : string
    Category : string[]
    Quantity : int
}

type InventoriesRequest = InventoryRequest[]

type InsertRes = {
    IRCode : int64
    Msg : string
}

type UpdateRes = {
    URCode : int64
    Msg : string
}

type Inventories =InventoryDetail[]

type RegisterRequest = {
    Email : string
    Password : string
    Company : string
}

[<CLIMutable>]
type User = {
    Id : BsonObjectId
    Email : string    
    Password : string
    Company : string
    DateJoin: string
    LastLogin : string
    Login : bool
}

type LoginRequest = {
    Email : string
    Password : string
}
type LoginSuccessResponse = {
    status : int
    lsrmsg : string
    email : string
    token : string
}
type LoginFailResponse = {
    status : int
    lfrmsg : string
}

type RegisterSuccessResponse = {
    status : int
    rsrmsg : string
}

type RegisterfailResponse = {
    status : int
    rfrmsg : string
}
