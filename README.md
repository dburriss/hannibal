# Hannibal

> Work in progress

- [x] Basic flow from request to console report
- [x] Timed<> Response
- [ ] More assertions on response and timing
- [ ] Mountebank helpers for testing
- [ ] Spawned request workers for parallel calls (add workers to tactic)
- [ ] Randomize
- [ ] form and url binding
- [ ] Markdown & CSV report and save
- [ ] Expand Tactics with ramp-up until response time increase

I am a HTTP DSL. Hannibal has 3 modules for different *Http* request/response, setup and execute a test *Plan*, and finally a module for executing *Reports* of the plan execution.

## Simple example with test plan and report

```fsharp
//..define urls..then

//health request
let health_test_request = get_from healthUrl

//contact form request
let contact_post_request =
    post_to formUrl
    |> with_body (randomize<ContactForm> |> to_form)
    |> with_header "key" "let me in" 
    |> with_header "correlationid" "abc"

//test plan
let contact_plan = 
    plan "contact form submit"
    |> step "Check health" (call health_test_request once)
    |> step "Spam contact form" (call contact_post_request (for_duration_of 2 seconds))

//execute test plan
let r = execute contact_plan

//assert against test plan execution results
let debriefing = 
    debrief r
    |> plan_result (should_be (status_code 200))
    |> step_result "Check health" (should_be (status_code 200))

//save assertion results to csv
format_debriefing debriefing as_text write_to_console
```

### Output

```console
========================================================================================
CONTACT FORM SUBMIT
========================================================================================
Check health      | Success | Calls Get "http://locahost/health/instance" Once
========================================================================================
Spam contact form | Success | Calls Post "http://localhost/contact" Duration (2,Seconds)
========================================================================================
I love it when a plan comes together.
========================================================================================
```

## HTTP

The Http module allows you to describe a `Request` and execute that request to get a `Response`. This does not need to be used for testing but could be used for any HTTP work, although it is built for readability not terseness.

### Example: Simple Get request/response

There are functions matching verbs you would use in HTTP. These functions return a Request. Executing the request will return the Response.

```fsharp
let request = get_from "http://google.com/"
let response = execute_request request
```

### Example: Post with headers, body, and encoding

```fsharp
let request = post_to formUrl
                |> with_body (randomize<ContactForm> |> to_form)
                |> with_header "key" "let me in" 
                |> with_header "correlationid" "abc"
                |> encoding_type "application/forms"
let response = execute_request request
```

### Tactics

Tactics come in many forms. The simplest is `Once`. This executes a request a single time. 

Then you have the `CountOf` tactic which allows you to specify a number in of times to call a `Request`.

Next you have the `Duration` tactic which allows you to specify a number in `Seconds`, `Minutes`, or `Hours`.

### Http: Important types and functions

```fsharp
//HTTP
type Request = {
    Resource: Resource
    Body: string option
    Headers: (string * string) list
    Encoding: Encoding
}

type Response = {
    RequestDescription: string
    StatusCode: int
    Content: string option
    Headers: (string * string) list
    Raw: HttpResponseMessage
}

// take a string and return a Request
get_from
post_to
put_to
delete_from
head_of
options_of
trace_of

//takes a Request, executes it, and returns the Response
execute_request

//TACTICS
type DurationUnit =
        | Seconds
        | Minutes
        | Hours

type Tactic =
    | Once
    | Duration of int * DurationUnit
    | CountOf of int

//takes Request and Tactic
execute_request_with_tactic
```

## Plan

Probably the most important part of Hannibal is his `Plan`. A plan allows you to compose `Step`s. A `Step` is a `Request` with an associated `Tactic`.

### Example: Test critical e-commerce features

```fsharp
let critical_site_features_plan = 
    plan "Critical Features"
    |> step "Health check" (call health_test_request once)
    |> step "Home page" (call home_request (for_duration_of 2 seconds))
    |> step "Checkout" (call cart_request (for_duration_of 2 seconds))

let result = execute critical_site_features_plan
```

### Debrief on Plan results

A `Debriefing` allows you to setup a series of assertions against the `Plan`, or specific Steps in the Plan.

For example we can assert that the health test should be *200 OK*.

```fsharp
let debriefing =
        debrief critical_site_features_plan
        |> step_result "Check health is OK" (should_be (status_code 200))
```

### Plan: Important types and functions

```fsharp
//PLAN
type Step = {
    Name: string
    Tactic: Tactic
    Request: Request
}

type Plan = {
    Name: string
    Steps: Step list
}

type StepExecutionResult = (Step * Response list) 
type PlanExecutionResult = (Plan * StepExecutionResult list)

//takes a name for the plan and returns a Plan
plan

//takes a string for the name of the step and a Request with a Tactic and the Plan to add the step to
step
```

## Report

> Currently the only report format is `as_text` with the output target being `write_to_console`.

```fsharp
format_debriefing debriefing as_text write_to_console
```