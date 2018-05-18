namespace Hannibal

module Http = 

    open System
    open System.Net.Http

    // furl
    let private addHeader (headers : Headers.HttpHeaders) (name, value : string) =
        headers.Add (name, value)

    let private addBody (req : HttpRequestMessage) headers body =
        req.Content <- new StringContent (body)
        let contentTypeHeader =
            headers |> List.tryFind (fun (n, _) -> n = "Content-Type")
        contentTypeHeader
        |> Option.iter (fun (_, v) -> req.Content.Headers.ContentType.MediaType <- v)

    let private result (t : System.Threading.Tasks.Task<_>) = t.Result

    let private composeMessage meth (url : Uri) headers body =
        let req = new HttpRequestMessage (meth, url)
        Option.iter (addBody req headers) body

        headers
        |> List.partition (fun (n, _) -> n = "Content-Type")
        |> snd
        |> List.iter (addHeader req.Headers)
        req

    //types

    type Encoding = 
        | UrlEncoded
        | MultipartFormData
        | TextPlain

    type Resource = 
        | Get of string
        | Post of string
        | Put of string
        | Delete of string

    type Request = {
        Resource: Resource
        Body: string option
        Headers: (string * string) list
        Encoding: Encoding
    }

    type DurationUnit = 
        | Seconds
        | Minutes
        | Hours

    type Tactic = 
        | Once
        | Duration of int * DurationUnit
        | CountOf of int


    type Step = {
        Name: string
        Tactic: Tactic
        Request: Request
    }

    type Call = Request * Tactic

    type Response = {
        RequestDescription: string
        StatusCode: int
        Content: string option
        Headers: (string * string) list
        Raw: HttpResponseMessage
    }

    // helpers
    let second = Seconds
    let seconds = Seconds
    let minute = Minutes
    let minutes = Minutes
    let hour = Hours
    let hours = Hours
    let to_form<'a> (obj:'a) = ""
    let randomize<'a> (obj:'a) = obj 
    let once = Once
    let for_duration_of (duration:int) (durationUnit:DurationUnit) = Duration (duration, durationUnit)
    let to_count_of count = CountOf count
    let to_timespan (time:int) (du:DurationUnit) =
        match du with
        | Seconds -> TimeSpan.FromSeconds (time |> float)
        | Minutes -> TimeSpan.FromMinutes (time |> float)
        | Hours -> TimeSpan.FromHours (time |> float)
    
    let private make_resource resource : Request =
        {
            Resource = resource
            Body = None
            Headers = List.Empty
            Encoding = UrlEncoded
        }

    //verb actions
    let get_from url : Request = make_resource (Get url)
    let post_to url : Request = make_resource (Post url)
    let put_to url : Request = make_resource (Put url)
    let delete url : Request = make_resource (Delete url)

    // request modifiers
    let with_body body (request:Request) = { request with Body = Some(body) }
    let with_header key value (request:Request) = { request with Headers = List.append request.Headers [(key, value)] }
    let encoding_type enctype request = { request with Encoding = enctype }
    
    let call request tactic : Call  = (request, tactic)

    let toResponse (request:Request) (msg:HttpResponseMessage) : Response =
        {
            RequestDescription = sprintf "%A" request.Resource
            StatusCode = msg.StatusCode |> int
            Content = if (msg.Content = null) then None else Some(msg.Content.ReadAsStringAsync().Result)
            Headers = msg.Headers |> Seq.collect (fun h -> (h.Value |> Seq.map (fun v -> (h.Key, v)))) |> Seq.toList
            Raw = msg
        }

    let private getRequest (request:Request) = 
        let (Get url) = request.Resource
        let headers = request.Headers
        use client = new HttpClient ()
        composeMessage Net.Http.HttpMethod.Get (Uri url) headers None
        |> client.SendAsync
        |> result
        |> toResponse request

    let execute_request request =
        match request.Resource with
        | Get _ -> getRequest request
        | _ -> failwithf "Resource %A not supported" request.Resource
 
    let execute_request_for_duration request time units =
        let clock = (fun () -> DateTimeOffset.UtcNow) 
        let ts = to_timespan time units
        let now = clock()
        let until = now + ts
        //TODO: time each
        //TODO: run multiple in parallel
        let rSeq = Seq.initInfinite (fun i -> execute_request request)
        let responses = new ResizeArray<Response>()
        let mutable i = 0
        while(clock() < until) do
            let response = rSeq |> Seq.item i
            responses.Add(response)
            i <- i + 1
        responses |> Seq.toList

    let execute_request_with_tactic request tactic = 
        match tactic with
        | Once -> [execute_request request]
        | CountOf n -> [1..n]|> List.toArray |> Array.Parallel.map (fun i -> execute_request request) |> Array.toList
        | Duration (0,u) -> failwithf "Tactic %A not supported" tactic
        | Duration (v,u) -> execute_request_for_duration request v u

    