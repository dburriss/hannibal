# Hannibal
I am a HTTP DSL

## Example usage

```fsharp
//..define urls..then

//health request
let health_test_request = get_from healthUrl

//contact form request
let contact_post_request =
    post_to formUrl
    //|> with_body (randomize<ContactForm> |> to_form)
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
