module HttpTests

open Xunit
open Hannibal.Http

let url = "https://google.com/"

[<Fact>]
let ``Calling google gets a response`` () =
    let g = get_from url
    let responses = execute_request_with_tactic g Once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``Calling google gets status code is 200`` () =
    let g = get_from url
    let responses = execute_request_with_tactic g Once
    Assert.True(responses.Head.StatusCode = 200)

[<Fact>]
let ``Calling google gets Content`` () =
    let g = get_from url
    let responses = execute_request_with_tactic g once
    Assert.True(responses.Head.Content.IsSome)

[<Fact>]
let ``Calling google twice gets 2 responses`` () =
    let g = get_from url
    let responses = execute_request_with_tactic g (to_count_of 2)
    Assert.True(responses.Length = 2)

[<Fact>]
let ``Calling google for 1 second calls more than once`` () =
    let g = get_from url
    let responses = execute_request_with_tactic g (for_duration_of 1 second)
    Assert.True(responses.Length > 1)

[<Fact>]
let ``post`` () =
    let p = post_to url
    let responses = execute_request_with_tactic p once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``put`` () =
    let p = put_to url
    let responses = execute_request_with_tactic p once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``delete`` () =
    let d = delete_from url
    let responses = execute_request_with_tactic d once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``head`` () =
    let h = head_of url
    let responses = execute_request_with_tactic h once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``options`` () =
    let o = options_of url
    let responses = execute_request_with_tactic o once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``trace`` () =
    let t = options_of url
    let responses = execute_request_with_tactic t once
    Assert.True(responses.Length = 1)

[<Fact>]
let ``content type`` () =
    Assert.True(false)