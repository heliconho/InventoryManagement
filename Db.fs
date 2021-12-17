namespace IM.Db
open MongoDB.Driver
open MongoDB.Bson
open IM.Model
open System
open System.Security.Cryptography
open System.Text
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.AspNetCore.Authorization
open Microsoft.Extensions.Configuration
open Microsoft.IdentityModel.Tokens

// Use mongodb due to multi-category

//this is the mongo connection module. currently hardcode. maybe put in the config file
module db = 
    let [<Literal>] user_name = "IM_Admin"
    let [<Literal>] password = "im_app_password"
    let connstr username password = sprintf "mongodb+srv://%s:%s@imapp.7ze5v.mongodb.net/IMAPP" username password
    let mongo = MongoClient(connstr user_name password)
    let db = mongo.GetDatabase "im_app"
    let inventoryCollection = db.GetCollection<InventoryDetail> "inventory"
    let userCollection = db.GetCollection<User> "user"
    let categoryCollection = db.GetCollection<Category> "category"

module shared = 
    let secretKey = "im_app_secert_key"

module hash = 
    open shared
    let hashing (password:string) = 
        use sha512 = SHA512.Create()
        ASCIIEncoding.UTF8.GetBytes password |> sha512.ComputeHash |> Seq.fold(fun hash byte -> hash + byte.ToString("x2")) secretKey
    let md5(password:string): string = 
        use md5 = MD5.Create()
        let data = sprintf "%s___%s" password secretKey |> ASCIIEncoding.UTF8.GetBytes
        (StringBuilder(), md5.ComputeHash(data)) ||> Array.fold(fun sb b -> sb.Append(b.ToString("x2"))) |> string        

module JwtToken = 
    open shared
    let secretKey = "im_app_secert_key"
    let buildToken email = 
        printfn "%s" email
        let issuer = "im_app_webapp.net"
        let claims = [|
            Claim(JwtRegisteredClaimNames.Sub, email);
            Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
        |]
        let signingKey = SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey))
        let expiresHour = 2
        let now:Nullable<DateTime> = Nullable<DateTime>(DateTime.UtcNow)
        let expiresTime = Nullable<DateTime>(now.Value.Add(TimeSpan(0,expiresHour,0,0)))
        let jwt = JwtSecurityToken(issuer,"all",claims,now,expiresTime,SigningCredentials(signingKey,SecurityAlgorithms.HmacSha512))
        let jwtSecurityTokenHandler = JwtSecurityTokenHandler()
        let encodedJwt = jwtSecurityTokenHandler.WriteToken(jwt)
        encodedJwt


module UserRepo =
    open hash
    open JwtToken
    let findUser (newUser : RegisterRequest) = db.userCollection.Find(fun u -> u.Email = newUser.Email).ToEnumerable()
    let findUserDetail (email : string) (password: string) = db.userCollection.Find(fun u -> (u.Email = email) && (u.Password = password )).ToEnumerable()
    let getUserIdFromEmail (email:string) = 
        let src = db.userCollection.Find(fun u -> u.Email = email).ToEnumerable() |> List.ofSeq
        src[0].Id.Value
    let insertUser (newUser: User) = db.userCollection.InsertOne(newUser)
    let updateUser (newUser : User) = 
        let filter = Builders<User>.Filter.Eq((fun u -> u.Email),newUser.Email)
        let update = Builders<User>.Update.Set((fun u -> u.Login),newUser.Login)
        db.userCollection.UpdateOne(filter,update)
    let register (newUser : RegisterRequest) = 
        task{
            let checkExists = findUser newUser |> List.ofSeq
            match checkExists with
            | [] -> //insert
                    let userRecord = {
                        Id = BsonObjectId(ObjectId.GenerateNewId());
                        Email = newUser.Email;
                        Password = newUser.Password;
                        Company = newUser.Company;
                        DateJoin = DateTimeOffset.Now.ToString("o"); 
                        LastLogin = DateTimeOffset.Now.ToString("o"); 
                        Login = false;
                    }
                    printfn "%O" userRecord
                    let insertResult = insertUser {userRecord with Password = (md5 userRecord.Password)} //hash password
                    return Ok {rsrmsg = "Register Successed"}
            | _ -> return Error { rfrmsg = sprintf "This Email Had been register with id: %s" (checkExists[0].Id.Value.ToString())}
        }

    let login loginObject = 
        let hashed = md5 loginObject.Password
        task{
            let check = findUserDetail loginObject.Email hashed |> List.ofSeq
            match check.IsEmpty with
            | false -> 
                let loginRes = updateUser {check[0] with Login = true; LastLogin = DateTimeOffset.Now.ToString("o"); }
                match loginRes.IsAcknowledged with
                | true -> return Ok {lsrmsg = "Login Success"; email = check[0].Email ;token =  Some(buildToken check[0].Email).Value }
                | false -> return Error {lfrmsg = "Login Failed";}
            | true -> return Error {lfrmsg = "Login Failed";}
        }

module InventoryRepo = 
    open hash
    open UserRepo
    let getAllInventory (email:string) = 
        task {
            let userId = db.userCollection.Find(fun u -> u.Email = email).ToEnumerable() |> List.ofSeq
            return db.inventoryCollection.Find(fun i -> i.User = userId[0].Id).ToEnumerable()
        }
    let findInventoryById (inventoryId : BsonObjectId) = db.inventoryCollection.Find(fun i -> i.Id.Value = inventoryId.Value).ToEnumerable() 
    let findInventoryBySKU (inventorySKU : string) = db.inventoryCollection.Find(fun i -> i.SKU = inventorySKU).ToEnumerable() 
    let insertInventory (newInventory:InventoryRequest) (email : string)=
        task{
            let inv = findInventoryBySKU newInventory.SKU |> List.ofSeq
            match inv with
            | [] -> //not found
                    let userId = BsonObjectId( getUserIdFromEmail email )
                    let newInv = {
                        Id = BsonObjectId(ObjectId.GenerateNewId());
                        InventoryName = newInventory.InventoryName;
                        Description = newInventory.Description;
                        SKU = newInventory.SKU;
                        Category = newInventory.Category;
                        Quantity = newInventory.Quantity;
                        DateCreated = DateTimeOffset.Now.ToString("o"); 
                        User = userId;
                        DateChecked = DateTimeOffset.Now.ToString("o"); 
                        Active = false;
                    }
                    db.inventoryCollection.InsertOne(newInv)
                    return Ok {IRCode = 0;Msg = sprintf "%s created success" newInv.InventoryName}
            | _ -> return Error {IRCode = 1;Msg = sprintf "%s created failed becuase SKU already exist" newInventory.InventoryName}

        }
        
    //this function should not work.
    let insertNInventory (newInventories:InventoriesRequest) (email:string)= //db.inventoryCollection.InsertMany(newInventories)
        task{
            let insertNResult =  newInventories |> Array.map(fun i -> insertInventory i email)
            return insertNResult
        }
    
    let updateInventory (newInventory : InventoryDetail) =
        let filter = Builders<InventoryDetail>.Filter.Eq((fun i -> i.Id),newInventory.Id)
        let update = Builders<InventoryDetail>.Update.
                        Set((fun i -> i.InventoryName), newInventory.InventoryName).
                        Set((fun i -> i.Description),newInventory.Description).
                        Set((fun i -> i.SKU),newInventory.SKU).
                        Set((fun i -> i.Category),newInventory.Category).
                        Set((fun i -> i.Quantity),newInventory.Quantity).
                        Set((fun i -> i.DateChecked),DateTimeOffset.Now.ToString("o")).
                        Set((fun i -> i.Active),newInventory.Active)
        let updateRes = db.inventoryCollection.UpdateOne(filter,update)
        match updateRes.IsAcknowledged with
        | true -> Ok { URCode = updateRes.ModifiedCount; Msg = sprintf "%s Update Success" newInventory.InventoryName}
        | false -> Error { URCode = updateRes.ModifiedCount; Msg = sprintf "%s Update Failed" newInventory.InventoryName}
    let updateNInventory (newInventories : Inventories) = 
        newInventories |> Array.map(fun i -> updateInventory i)
    let removeInventory (inventoryId : BsonObjectId) = 
        let filter = Builders<InventoryDetail>.Filter.Eq((fun i -> i.Id),inventoryId)
        let update = Builders<InventoryDetail>.Update.Set((fun i -> i.Active),false)
        db.inventoryCollection.UpdateOne(filter,update)
