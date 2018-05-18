module Program

open Hannibal.Http
open Hannibal.Plan
open Hannibal.Reports

//TEST SYNTAX
//form data
type ContactForm = { name:string; email:string }

let [<EntryPoint>] main _ = 

    //health request
    let health_test_request = get_from "locahost:5001/health"

    //contact form request
    let contact_post_request =
        post_to "http://locahost:5001/contact"
        |> with_body (randomize<ContactForm> |> to_form)
        |> with_header "key" "let me in" 
        |> with_header "correlationid" "abc"

    //test plan
    let contact_plan = 
        plan "contact form submit"
        |> step "check health" (call health_test_request once)
        |> step "spam contact form" (call contact_post_request (for_duration_of 2 minutes))

    //execute test plan
    let r = execute contact_plan

    //assert against test plan execution results
    let debriefing = 
        debrief r
        |> plan_result (should_all_be (status_code 200))
        //|> step_result "check health" (should_be (status_code 200))


    //save assertion results to csv
    format_debriefing as_csv 
    |> save_to "report.csv"
    0
