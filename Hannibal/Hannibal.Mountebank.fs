namespace Hannibal

module Mountebank =
    open Http
    open System.Net.Http
    open System

    let mutable Port = 4545
    let mutable Url = "http://localhost:2525/"
    let mutable ConfigurationPort = 2525
        
    type Imposter = {
        Request: Request
        DefaultResponse: Response option
        Responses: Response list
    }

    let for_call resource : Imposter =
        {
            Request = Http.make_request resource
            DefaultResponse = None
            Responses = []
        }

    let send_status_code status imposter =
        let request = imposter.Request
        let response = {
            RequestDescription = sprintf "%A" request.Resource
            StatusCode = status
            Content = None
            Headers = []
            Raw = new HttpResponseMessage()
            ResponseTime = TimeSpan.Zero
        }
        { imposter with Responses = List.append imposter.Responses [response]}

    let fake_it (imposter:Imposter) : unit = 

//        let p = post_to Url
//                |> with_body 
//                    "
//{
//  \"port\": 4545,
//  \"protocol\": \"http\",
//  \"stubs\": [{
//    \"responses\": [
//      { \"is\": { \"statusCode\": 400 }}
//    ],
//    \"predicates\": [{
//      \"and\": [
//        {
//          \"equals\": {
//            \"path\": \"/test\",
//            \"method\": \"GET\"
//          }
//        }
//      ]
//    }]
//  }]
//}"
//        let response = execute_request p
//        if(response.StatusCode <> 200) then failwith (response.Content |> string)
        let client = new MbDotNet.MountebankClient()
        client.DeleteImposter(Port)
        let i = client.CreateHttpImposter(Nullable<int>(Port), "Hannibal")
        i.AddStub().OnPathAndMethodEqual("/idontexist", MbDotNet.Enums.Method.Get).ReturnsStatus(Net.HttpStatusCode.OK) |> ignore
        client.Submit(i)
        ()