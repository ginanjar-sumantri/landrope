namespace fimport.blmlunas 

    open System
    open System.Collections.Generic

    // ----------------------------------------------------------------------------

    module MyImperative =
      type ImperativeResult<'T> = 
        | ImpValue of 'T
        | ImpJump of int * bool
        | ImpNone 
  
      type Imperative<'T> = unit -> ImperativeResult<'T>

      // ----------------------------------------------------------------------------
  
      type ImperativeBuilder() = 
        member x.Combine(a, b) = (fun () ->
          match a() with 
          | ImpNone -> b() 
          | res -> res)
        member x.Delay(f:unit -> Imperative<_>) = (fun () -> f()())
        member x.Return(v) : Imperative<_> = (fun () -> ImpValue(v))
        member x.Zero() = (fun () -> ImpNone)
        member x.Run<'T>(imp) = 
          match imp() with
          | ImpValue(v) -> v
          | ImpJump _ -> failwith "Invalid use of break/continue!"
          | _ when typeof<'T> = typeof<unit> -> Unchecked.defaultof<'T>
          | _ -> failwith "No value has been returend!"

      // ----------------------------------------------------------------------------
      // Add special 'Combine' for loops and implement loops
      // Add 'Bind' to enable using of 'break' and 'continue'

      type ImperativeBuilder with 
        member x.CombineLoop(a, b) = (fun () ->
          match a() with 
          | ImpValue(v) -> ImpValue(v) 
          | ImpJump(0, false) -> ImpNone
          | ImpJump(0, true)
          | ImpNone -> b() 
          | ImpJump(depth, b) -> ImpJump(depth - 1, b))
        member x.For(inp:seq<_>, f) =
          let rec loop(en:IEnumerator<_>) = 
            if not(en.MoveNext()) then x.Zero() else
              x.CombineLoop(f(en.Current), x.Delay(fun () -> loop(en)))
          loop(inp.GetEnumerator())
        member x.While(gd, body) = 
          let rec loop() =
            if not(gd()) then x.Zero() else
              x.CombineLoop(body, x.Delay(fun () -> loop()))
          loop()         
        member x.Bind(v:Imperative<unit>, f : unit -> Imperative<_>) = (fun () ->
          match v() with
          | ImpJump(depth, kind) -> ImpJump(depth, kind)
          | _ -> f()() )
     
      let imperative = new ImperativeBuilder()  
      let BREAK = (fun () -> ImpJump(0, false))
      let CONTINUE = (fun () -> ImpJump(0, true))
      let BREAKN(n) = (fun () -> ImpJump(n, false))
      let CONTINUEN(n) = (fun () -> ImpJump(n, true))

      // ----------------------------------------------------------------------------
      // Using 'return' in the middle of the code

      let test = imperative {
          return 0
          return 1
        }
  
      let validateName(arg:string) = imperative {
        if (arg = null) then return false
        let idx = arg.IndexOf(" ")
        if (idx = -1) then return false
        let name = arg.Substring(0, idx)
        let surname = arg.Substring(idx + 1, arg.Length - idx - 1)
        if (surname.Length < 1 || name.Length < 1) then return false
        if (Char.IsLower(surname.[0]) || Char.IsLower(name.[0])) then return false
        return true }

      validateName(null)  
      validateName("Tomas")  
      validateName("Tomas Petricek")  

      let exists f inp = imperative {
        for v in inp do 
          printfn "testing %A" v
          if f(v) then return true
        return false }

      [ 1 .. 10 ] |> exists (fun v -> v % 3 = 0)

      let readFirstName() = imperative {
        while true do
          let name = Console.ReadLine()
          if (validateName(name)) then
            return name
          printfn "That's not a valid name! Try again..." }

      // ----------------------------------------------------------------------------
      // Using 'break' and 'continue' to jump inside loops

      imperative { 
        for x in 1 .. 10 do 
          if (x % 3 = 0) then do! CONTINUE
          printfn "number = %d" x }
    
      imperative { 
        for x in 1 .. 10 do 
          if (x % 7 = 0) then do! BREAK
          printfn "number = %d" x }

      imperative {
        if 1 = 2 then 
          return 42 }
    
      imperative { 
        let result = ref None
        for x in [ 10 .. 20 ] do
          printfn "x = %d" x
          for y in [ 1 .. 5 ] do
            if (x + y < 20) then do! CONTINUEN(1)
          for y in [ 1 .. 5 ] do
            printfn "- y = %d" y
            if (x * y > 80) then 
              result := Some(x, y)
              do! BREAKN(1)
        printfn "result = %A" (!result) }    
