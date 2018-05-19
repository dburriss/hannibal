namespace Hannibal

module Reports = 

    open Hannibal.Http
    open Hannibal.Plan
    open System.Text

    //reporting
    let format_debriefing debriefing format report = 
        let r = format debriefing
        r |> report |> ignore

    // format
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
        sprintf "%s | %s | Calls %A %A" step.Name isSuc res step.Tactic

    let private line = "====================================================================================="
    let as_csv = ()
    let as_text (debriefing:Debriefing) = 

        let append (sb:StringBuilder) (s:string) = sb.Append(s) |> ignore
        let s = StringBuilder()
        let p str = append s str

        let n = System.Environment.NewLine
        let pln() = 
            append s line
            append s n
        
        let head (s:string) = 
            pln()
            p (s.ToUpper())
            p n
            pln()

        let row (s:string) = 
            p s
            p n
            pln()

        let body (d:Debriefing) = (snd d.PlanExecutionResult) |> List.iter (fun sre -> row(sreS d sre))

        let footer (d:Debriefing) = 
            let fs = if(d.Failures.Length = 0) then "I love it when a plan comes together." else "Back to the drawing board..."
            p fs
            p n
            pln()

        head (planName debriefing.PlanExecutionResult)
        body debriefing
        footer debriefing
        s.ToString()
            

        

    //report pipeline
    let save_to_file path report = ()    
    let write_to_console report = printf "%s" report |> ignore