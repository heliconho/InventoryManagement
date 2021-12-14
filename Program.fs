open System
open IM.Handler.UserHanlder
open IM.Handler.InventoryHandler
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.Hosting
open Giraffe
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open System.Security.Claims
open Microsoft.IdentityModel.Tokens
open System.Text
open System.IdentityModel.Tokens.Jwt
open Microsoft.AspNetCore.Cors.Infrastructure


let secretKey = "im_app_secert_key"
let authorize = requiresAuthentication(challenge JwtBearerDefaults.AuthenticationScheme)
let buildToken email = 
    let issuer = "im_app_webapp.net"
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email);
        Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString());
    |]
    let signingKey = SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey))
    let expiresHour = 2
    let now:Nullable<DateTime> = Nullable<DateTime>(DateTime.UtcNow)
    let expiresTime = Nullable<DateTime>(now.Value.Add(TimeSpan(0,expiresHour,0,0)))
    let jwt = JwtSecurityToken(issuer,"all",claims,now,expiresTime,SigningCredentials(signingKey,SecurityAlgorithms.HmacSha512))
    let jwtSecurityTokenHandler = JwtSecurityTokenHandler()
    let encodedJwt = jwtSecurityTokenHandler.WriteToken(jwt)
    encodedJwt

let webApp = 
    choose [
        POST >=> 
            choose [
                route "/api/v1/user/register" >=> registerHandler
                route "/api/v1/user/login" >=> loginHandler
                
                route "/api/v1/inventory/create" >=> authorize >=> createHandler
                // route "/api/v1/inventory/read/%s" >=> text "read id"
            ]
        GET >=> 
            choose [
                route "/api/v1/inventory/read" >=> authorize >=> getHandler
            ]
        setStatusCode 404 >=> text "Not Found"
    ]

let errorHandler (ex:Exception)(logger:ILogger) = 
    logger.LogError(EventId(), ex, "Failed to execute request due to error")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureLogging (builder:ILoggingBuilder) = 
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

let configureApp(app:IApplicationBuilder) = 
    app.UseCors(new Action<_>(fun (b: CorsPolicyBuilder) -> b.AllowAnyHeader() |> ignore; b.AllowAnyMethod() |> ignore))
        .UseAuthentication()
        .UseGiraffeErrorHandler(errorHandler)
        .UseGiraffe(webApp)

let configureServices(services: IServiceCollection) = 
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
            options.TokenValidationParameters <- TokenValidationParameters(
                ValidateActor = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "im_app_webapp.net",
                ValidAudience = "all",
                IssuerSigningKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            )) |> ignore
    services.AddGiraffe() |> ignore
    services.AddCors() |> ignore


//App Start
[<EntryPoint>]
let main _ = 
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices) |> ignore
        )
        .Build()
        .Run()
    0