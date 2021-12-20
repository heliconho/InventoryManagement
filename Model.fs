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

type InventoryResponse = {
    code : int64
    msg : string
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

type Response = {
    status : int64
    msg : string
    email : string
    token : string }

    