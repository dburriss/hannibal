﻿module PlanTests

open Xunit
open Hannibal.Http
open Hannibal.Plan
open Hannibal.Mountebank

let url = "http://localhost:4545/"

[<Fact>]
let ``plan name is set`` () =
    let get_google = get_from url
    let google_plan =
        plan "contact form submit"
        |> step "get google" (call get_google once)
    Assert.Equal("contact form submit", google_plan.Name)

[<Fact>]
let ``plan has results for every step`` () =
    let get_google = get_from url
    let google_plan =
        plan "contact form submit"
        |> step "get google" (call get_google once)
        |> step "get google again" (call get_google (to_count_of 2))
    Assert.Equal(2, google_plan.Steps.Length)


[<Fact>]
let ``execute has responses for every step`` () =
    let get_google = get_from url
    let google_plan =
        plan "contact form submit"
        |> step "get google" (call get_google once)
        |> step "get google again" (call get_google (to_count_of 2))

    let results = execute google_plan
    let plan, sres = results
    let responses = sres |> List.collect (fun (_, rs) -> rs)
    Assert.Equal(2, sres.Length)
    Assert.Equal(3, responses.Length)

[<Fact>]
let ``assert all steps are 200`` () =
    let get_google = get_from url
    let google_plan =
        plan "contact form submit"
        |> step "get google" (call get_google once)
        |> step "get google twice" (call get_google (to_count_of 2))

    let results = execute google_plan
    let debriefing = 
        debrief results
        |> plan_result (should_be (status_code 200))

    Assert.Equal(3,debriefing.Successes.Length)
    Assert.Equal(0,debriefing.Failures.Length)

[<Fact>]
let ``has failure if 404`` () =
    let get_google = get_from url
    let i_dont_exist = get_from (sprintf "%sidontexist" url)
    let google_plan =
        plan "contact form submit"
        |> step "get google" (call i_dont_exist once)
        |> step "get google twice" (call get_google (to_count_of 2))

    let results = execute google_plan
    let debriefing = 
        debrief results
        |> plan_result (should_be (status_code 200))

    Assert.Equal(2,debriefing.Successes.Length)
    Assert.Equal(1,debriefing.Failures.Length)

[<Fact>]
let ``assert success with success`` () =
    let get_google = get_from url
    let google_plan =
        plan "contact form submit"
        |> step "get google twice" (call get_google (to_count_of 2))

    let results = execute google_plan
    let debriefing = 
        debrief results
        |> plan_result (should_be (status_code 200))

    Assert.True(is_success debriefing)

    
[<Fact>]
let ``assert success with failure`` () =
    let get_url = get_from url
    
    let i_dont_exist_url = sprintf "%sidontexist" url
    let i_dont_exist = get_from i_dont_exist_url
    for_call (Get i_dont_exist_url)
    |> send_status_code 404
    |> fake_it

    let google_plan =
        plan "contact form submit"
        |> step "get google" (call i_dont_exist once)
        //|> step "get google twice" (call get_url (to_count_of 2))

    let results = execute google_plan
    let debriefing = 
        debrief results
        |> plan_result (should_be (status_code 200))

    Assert.False(is_success debriefing)
    