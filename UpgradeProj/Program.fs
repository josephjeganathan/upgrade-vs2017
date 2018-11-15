open System
open FSharp.Data
open System.IO

type OldProj = XmlProvider<"../sample-old-proj.csproj">
type Vs2017Proj = XmlProvider<"../sample-new-proj.csproj">
type Packages = XmlProvider<"../sample-packages.config">

[<EntryPoint>]
let main argv =

    let packages content =            
        
        Packages.Parse(content).Packages
        |> Seq.map (fun x -> Vs2017Proj.PackageReference(x.Id, x.Version))
        |> Seq.toArray
        |> fun x -> Vs2017Proj.ItemGroup(x, [||], [||])

    let projectReference content =
        
        let projRefs =
            OldProj.Parse(content).ItemGroups
            |> Seq.filter (fun x -> x.ProjectReferences.Length > 0)
            |> Seq.map (fun x -> x.ProjectReferences)
            |> Seq.concat
            |> Seq.map (fun x -> Vs2017Proj.ProjectReference(x.Include))
            |> Seq.toArray
        match projRefs.Length with
        | 0 -> None
        | _ -> Some <| Vs2017Proj.ItemGroup([||], projRefs, [||])
    
    let upgrade projectFile =

        printfn "Upgrading %s" (FileInfo(projectFile).Name)

        let getContent file = 
            match File.Exists(file) with
            | true -> File.ReadAllText file |> Some
            | false -> None

        let dir = FileInfo(projectFile).Directory.FullName
        let packageFile = dir + "/packages.config"

        let itemGroups =
            [| 
                packageFile |> getContent |> Option.map packages
                projectFile |> getContent |> Option.bind projectReference
            |] 
            |> Array.filter Option.isSome 
            |> Array.map Option.get

        Vs2017Proj.Project(
            "Microsoft.NET.Sdk", 
            Vs2017Proj.PropertyGroup("net462"), 
            itemGroups)
        |> fun x -> File.WriteAllText(projectFile, x.ToString(), Text.Encoding.UTF8)

        let deleteIfExists file = if File.Exists(file) then File.Delete(file)

        Directory.Delete(dir + "/Properties", true)

        [
            packageFile
            dir + "/app.Debug.config"
            dir + "/app.Release.config"
            dir + "/app.config"
        ] |> List.iter deleteIfExists

    Directory.GetFiles("C:\Code\CreditClear\Engine\Endpoints\Console", "*.csproj", EnumerationOptions(RecurseSubdirectories = true))
    |> Seq.iter upgrade
    
    printfn "Done!"
    Console.ReadLine() |> ignore
    
    0
    