namespace Hannibal

module Reports = 

    open Hannibal.Plan
    open System.Text
    open System

    //reporting
    let format_debriefing debriefing format report = 
        let r = format debriefing
        r |> report |> ignore

    // format
    let trunc l (s:string) = 
        let x = min l s.Length
        s.Substring(0, x).PadRight(l)

    let private planName (per:PlanExecutionResult) =
        let plan, _ = per
        plan.Name

    let private stepName (ser:StepExecutionResult) =
        let step, _ = ser
        step.Name

    let private stepSucceeded stepName (debriefing:Debriefing) =
        let isSuccess = debriefing.Successes |> List.tryFind(fun a -> a.Step.Name = stepName) |> Option.isSome
        isSuccess

    let sreS d (ser:StepExecutionResult) =
        let step, rs  = ser
        let isSuc = if(stepSucceeded step.Name d) then "Success" else "Fail"
        let res = step.Request.Resource
        let avg = rs    |> List.map (fun r -> r.ResponseTime) |> Statistics.calculateAverage
                        |> Option.defaultValue TimeSpan.Zero
        sprintf "|%-20s | %5s | %10s | Calls %s|" 
                (trunc 20 step.Name) 
                (trunc 7 isSuc) 
                (trunc 10 (sprintf "%f s" avg.TotalSeconds))
                (trunc 64 (sprintf "%A %A" res step.Tactic))

    let private line = new System.String('-', 118)
    let as_csv = ()
    let as_text (debriefing:Debriefing) = 
        
        let append (sb:StringBuilder) (s:string) = sb.Append(s) |> ignore
        let sb = StringBuilder()
        let print str = append sb str

        let nl = System.Environment.NewLine
        let printline() = 
            append sb line
            append sb nl
        
        let head (s:string) = 
            printline()
            print (trunc 100 (s.ToUpper()))
            print nl
            printline()

        let row (s:string) = 
            print s
            print nl
            printline()

        let body (d:Debriefing) = (snd d.PlanExecutionResult) |> List.iter (fun sre -> row(sreS d sre))

        let footer (d:Debriefing) = 
            let fs = if(d.Failures.Length = 0) then "I love it when a plan comes together." else "Back to the drawing board..."
            print fs
            print nl
            printline()

        head (planName debriefing.PlanExecutionResult)
        body debriefing
        footer debriefing
        sb.ToString()
            

        

    //report pipeline
    let save_to_file path report = ()    
    let write_to_console report = printf "%s" report |> ignore