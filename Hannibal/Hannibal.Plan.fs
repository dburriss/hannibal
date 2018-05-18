namespace Hannibal

module Plan =
    open System
    open Http

    //helper
    let isOk = function
    | Ok _ -> true
    | Error _ -> false

    let isError r = not (isOk r)

    //plan types
    type StepExecutionResult = (Step * Response list) 
    type PlanExecutionResult = StepExecutionResult list

    type Plan = {
        Name: string
        Steps: Step list
    }

    // plan functions
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

    let execute (plan:Plan) : PlanExecutionResult = 
        let executeStep step = (step, (execute_request_with_tactic step.Request step.Tactic))
        let stepResults = plan.Steps |> List.map (fun s -> executeStep s)
        stepResults    


    // debrief types
    
    type AssertionResult = {
        Description: string
        Step: Step
        Response: Response
    }

    type AssertionTarget<'a> = {
        Description: string
        Assert: 'a -> Result<AssertionResult,AssertionResult> list
    }

    //need to build up assert that have a function for assertion over specific range of types

    type AssertAgainst =
        | StepAssertion of AssertionTarget<StepExecutionResult>
        | PlanAssertion of AssertionTarget<PlanExecutionResult>


    type Debriefing = {
        PlanExecutionResult : PlanExecutionResult
        Successes: AssertionResult list
        Failures: AssertionResult list
    }  
    
    type Check<'a> = (string * ('a -> PlanExecutionResult -> Result<AssertionResult,AssertionResult> list) * 'a)

    //debrief
    let debrief (result:PlanExecutionResult) : Debriefing = 
        {
            PlanExecutionResult = result
            Successes = List<AssertionResult>.Empty
            Failures = List<AssertionResult>.Empty
        }

    let plan_result (assertion:AssertionTarget<PlanExecutionResult>) (debriefing:Debriefing) : Debriefing = 
        let assertions = assertion.Assert debriefing.PlanExecutionResult
        let split (err, ok) result =
            match result with
            | Ok x -> (err, ok |> List.append [x])
            | Error x -> (err |> List.append [x], ok)

        let (failures,successes) = assertions |> List.fold (fun s r -> split s r) ([],[])
        
        { debriefing with Successes = List.append debriefing.Successes successes ; Failures = List.append debriefing.Failures failures }

    let should_all_be<'a> ((desc, check, expected):Check<'a>): AssertionTarget<PlanExecutionResult> = 
        {
            Description = desc
            Assert = check expected
        }

    let step_result stepName assertion (debriefing:Debriefing) : Debriefing = 
        let stepXResult = debriefing.PlanExecutionResult |> List.find (fun (s,rs) -> s.Name = stepName)
        debriefing
    
    let should_be = ()

    

    let status_code (expected:int) : Check<int> = 
        let desc:string = sprintf "Status Code %i" expected
        let f (x:int) (result:PlanExecutionResult): Result<AssertionResult,AssertionResult> list =
            result |> List.collect (fun (s, rs) -> rs |> List.map (fun resp -> 
                                                    if(resp.StatusCode = x) then 
                                                        Ok { 
                                                                Step = s; 
                                                                Description = sprintf "Success: Status Code matches: %i" expected; 
                                                                Response = resp }
                                                    else Error { 
                                                                    Step = s; 
                                                                    Description = sprintf "Failure: Status Code mismatch. Expected %i but found %i" expected x; 
                                                                    Response = resp }
                                                )
                                )

        (desc, f, expected)
 
    let is_success (debriefing:Debriefing) =
        let failureCount = debriefing.Failures |> List.length
        if (failureCount > 0) then false else true//failwith (sprintf "%i assertion failures" failureCount)
