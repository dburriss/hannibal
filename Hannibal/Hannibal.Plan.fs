namespace Hannibal

module Plan = 
    open Http

    type PlanResult =
        | Planning
        | InProgress
        | Complete of (Step * Response list) list
        | Cancelled

    type Plan = {
        Name: string
        Steps: Step list
    }
    
    // plan
    let plan name =
        {
            Name = name
            Steps = List.empty
        }
        
    let step name (request, tactic) plan = 
        let step = {
            Name = name
            Tactic = tactic
            Request = request
        }
        { plan with Steps = List.append plan.Steps [step] }

    let execute (plan:Plan) = 
        let executeStep step = (step, (execute_request_with_tactic step.Request step.Tactic))
        let stepResults = plan.Steps |> List.map (fun s -> executeStep s)
        Complete stepResults    

    //debrief
    let debrief (result:PlanResult) = ()
    let should_be = ()
    let should_only_be = ()
    let step_result assertion expected planResult = ()
    let status_code expected actual = ()
