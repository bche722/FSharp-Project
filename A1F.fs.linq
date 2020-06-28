<Query Kind="FSharpProgram">
  <Reference Relative="Hopac.Core.dll">C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\Part1\A1\Assignment 1\Hopac.Core.dll</Reference>
  <Reference Relative="Hopac.dll">C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\Part1\A1\Assignment 1\Hopac.dll</Reference>
  <Reference Relative="Hopac.Platform.dll">C:\Users\Baiwei\Dropbox\UniversityResources\COMPSCI734\Part1\A1\Assignment 1\Hopac.Platform.dll</Reference>
</Query>

// -----------------Library--------------------

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.IO
open Hopac
open Hopac.Core
open Hopac.Infixes

// -----------------Global Variale--------------

type Message =
     | FromWest of int[]
     | FromNorth of int[]

let actResult = TaskCompletionSource<int> ()

let cspResult = TaskCompletionSource<int> ()

let mutable tempResult = Array2D.zeroCreate<TaskCompletionSource<int>> 0 0 

// -----------------Timer----------------------

let tid () = Thread.CurrentThread.ManagedThreadId

let duration f = 
    let timer = new Stopwatch()
    timer.Start()
    let res = f()
    timer.Stop()
    printfn "... [%d] duration: %i ms" (tid()) timer.ElapsedMilliseconds
    res
    
// -----------------SEQ----------------------

let seq (str1:string) (str2:string) : int =
    let z1, z2 = "?" + str1, "?" + str2
    let n1, n2 = z1.Length, z2.Length
    
    let d0 = Array.create n1 0
    let d1 = Array.create n1 0
    let d2 = Array.create n1 0
    
    //for k = 2 to n1+n2-2 do
    let rec loop k (d0: int[]) (d1: int[]) (d2: int[]) =
        let lo = if k < n2 then 1 else k-n2+1 
        let hi = if k < n1 then k-1 else n1-1
                    
        for i = lo to hi do
            if z1.[i] = z2.[k-i] 
            then d2.[i] <- d0.[i-1] + 1 
            else d2.[i] <- max d1.[i-1] d1.[i]
        
        if k < n1+n2-2 then loop (k+1) d1 d2 d0
        else d2.[n1-1]
    
    loop 2 d0 d1 d2

// -----------------Split----------------------

let split (str1:string) (str2:string) (div1:int) (div2:int) =
    let n1, n2 = str1.Length, str2.Length
    
    let split' n div =
        let q, r = n/div, n%div
        let l = List.replicate r (q+1) @ if q = 0 then [] else List.replicate (div-r) q
        let b = l |> List.scan (fun s n -> s+n) 0
        let z = Seq.zip b l
        Seq.toArray z
     
    let z1, z2 = split' n1 div1, split' n2 div2

    let str1', str2' = "?" + str1, "?" + str2
    
    let str1s = z1 |> Array.map (fun (b, l) -> str1'.Substring (b, l+1))
    let str2s = z2 |> Array.map (fun (b, l) -> str2'.Substring (b, l+1))
    
    (str1s, str2s)
    
// -----------------Actor----------------------

let actor (i1:int) (i2:int) (str1:string) (str2:string) (actors:MailboxProcessor<Message>[,]) = 
    MailboxProcessor<Message>.Start <| fun inbox ->
        async {
            let b1 = Array2D.length1 actors
            let b2 = Array2D.length2 actors
            
            let mutable n = Array.zeroCreate<int> 0
            let mutable w = Array.zeroCreate<int> 0
            
            let! m = inbox.Receive () 
            match m with
            | FromNorth t -> n <- t
            | FromWest t -> w <- t

            let! m' = inbox.Receive () 
            match m' with
            | FromNorth t -> n <- t
            | FromWest t -> w <- t
            
            let rows, cols = str1.Length, str2.Length
            let d0 = Array.create rows 0
            let d1 = Array.create rows 0
            let d2 = Array.create rows 0
            
            d0.[0] <- n.[0]
            d1.[0] <- n.[1]
            d1.[1] <- w.[1]
            
            let rec loop k (d0: int[]) (d1: int[]) (d2: int[]) =
                let lo = if k < cols then 1 else k-cols+1 
                let hi = if k < rows then k-1 else rows-1
                
                if k < cols then d2.[0] <- n.[k]
                if k < rows then d2.[k] <- w.[k]
                     
                for i = lo to hi do
                    if str1.[i] = str2.[k-i] 
                    then d2.[i] <- d0.[i-1] + 1 
                    else d2.[i] <- max d1.[i-1] d1.[i]
                
                if k >= rows-1 then n.[k-rows+1] <- d2.[rows-1]
                if k >= cols-1 then w.[k-cols+1] <- d2.[k-cols+1]
                
                if k < rows+cols-2 then loop (k+1) d1 d2 d0
                else d2.[rows-1]
            
            let r = loop 2 d0 d1 d2
            
            tempResult.[i1,i2].SetResult (r)
            
            if i1 = b1-1 && i2 = b2-1 then 
                actResult.SetResult (r)
            else   
                if i1 < b1-1 then actors.[i1+1, i2].Post (FromNorth n) // Post to South
                if i2 < b2-1 then actors.[i1, i2+1].Post (FromWest w)  // Post to East            
        }

// -----------------ACT----------------------
let act (str1:string) (str2:string) (b1:int) (b2:int) = 

    let actors = Array2D.zeroCreate<MailboxProcessor<Message>> b1 b2
    tempResult <- Array2D.zeroCreate<TaskCompletionSource<int>> b1 b2
    
    let (str1s,str2s) = split str1 str2 b1 b2 
    
    //Create actors
    for i1 = 0 to b1-1 do
        for i2 = 0 to b2-1 do
            tempResult.[i1,i2] <- TaskCompletionSource<int> ()
            actors.[i1, i2] <- actor i1 i2 str1s.[i1] str2s.[i2] actors
    
    //Provide init message
    for i2 = 0 to b2-1 do 
        actors.[0, i2].Post (FromNorth (Array.zeroCreate<int> (str2s.[i2].Length)))
        
    for i1 = 0 to b1-1 do 
        actors.[i1, 0].Post (FromWest (Array.zeroCreate<int> (str1s.[i1].Length)))

    actResult.Task.Result
    
// -----------------Channel Agent----------------------

let agent (i1:int) (i2:int) (str1:string) (str2:string) (channels:Ch<Message>[,])= 
    job {
            let b1 = Array2D.length1 channels
            let b2 = Array2D.length2 channels
            
            let mutable n = Array.zeroCreate<int> 0
            let mutable w = Array.zeroCreate<int> 0

            let! m = channels.[i1,i2]
            match m with
            | FromNorth t -> n <- t
            | FromWest t -> w <- t
   
            let! m' = channels.[i1,i2]
            match m' with
            | FromNorth t -> n <- t
            | FromWest t -> w <- t

            let rows, cols = str1.Length, str2.Length
            let d0 = Array.create rows 0
            let d1 = Array.create rows 0
            let d2 = Array.create rows 0

            d0.[0] <- n.[0]
            d1.[0] <- n.[1]
            d1.[1] <- w.[1]

            let rec loop k (d0: int[]) (d1: int[]) (d2: int[]) =
                let lo = if k < cols then 1 else k-cols+1 
                let hi = if k < rows then k-1 else rows-1
                
                if k < cols then d2.[0] <- n.[k]
                if k < rows then d2.[k] <- w.[k]
                     
                for i = lo to hi do
                    if str1.[i] = str2.[k-i] 
                    then d2.[i] <- d0.[i-1] + 1 
                    else d2.[i] <- max d1.[i-1] d1.[i]
                
                if k >= rows-1 then n.[k-rows+1] <- d2.[rows-1]
                if k >= cols-1 then w.[k-cols+1] <- d2.[k-cols+1]
                
                if k < rows+cols-2 then loop (k+1) d1 d2 d0
                else d2.[rows-1]

            let r = loop 2 d0 d1 d2
            
            tempResult.[i1,i2].SetResult (r)
            
            if i1 = b1-1 && i2 = b2-1 then 
                cspResult.SetResult (r)
            else   
                if i1 < b1-1 then do! channels.[i1+1, i2] *<- (FromNorth n) // Post to South
                if i2 < b2-1 then do! channels.[i1, i2+1] *<- (FromWest w)  // Post to East          
        }    
// -----------------CSP----------------------
let csp (str1:string) (str2:string) (b1:int) (b2:int) = 

    let channels = Array2D.create<Ch<Message>> b1 b2 (Ch<Message>())
    tempResult <- Array2D.zeroCreate<TaskCompletionSource<int>> b1 b2
    let (str1s,str2s) = split str1 str2 b1 b2 

    //Create jobs
    for i1 = 0 to b1-1 do
        for i2 = 0 to b2-1 do
            channels.[i1,i2] <- Ch<Message>()
            tempResult.[i1,i2] <- TaskCompletionSource<int> ()
            start (agent i1 i2 str1s.[i1] str2s.[i2] channels)

    //Provide init message
    let setup = job {
        for i2 = 0 to b2-1 do 
            do! channels.[0, i2] *<- (FromNorth (Array.zeroCreate<int> (str2s.[i2].Length)))
        
        for i1 = 0 to b1-1 do 
            do! channels.[i1, 0] *<- (FromWest (Array.zeroCreate<int> (str1s.[i1].Length)))
    }

    run setup
    cspResult.Task.Result
    
// -------------------------

let r = act "abcd" "awkff" 2 3
printfn "ACT: %d" r
// -------------------------