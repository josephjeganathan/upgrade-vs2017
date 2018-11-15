open System
open FSharp.Data
open System.IO

type OldProj = XmlProvider<"../sample-old-proj.csproj">
type NewProj = XmlProvider<"../sample-new-proj.csproj">
type Packages = XmlProvider<"../sample-packages.config">

[<EntryPoint>]
let main argv =

    let packages content =
        
        Packages.Parse(content).Packages
        |> Seq.map (fun x -> NewProj.PackageReference(x.Id, x.Version))
        |> Seq.toArray
        |> fun x -> NewProj.ItemGroup(x, [||], [||])

    let projectReference content =
        
        OldProj.Parse(content).ItemGroups
        |> Seq.filter (fun x -> x.ProjectReferences.Length > 0)
        |> Seq.map (fun x -> x.ProjectReferences)
        |> Seq.concat
        |> Seq.map (fun x -> NewProj.ProjectReference(x.Include))
        |> Seq.toArray
        |> fun x -> NewProj.ItemGroup([||], x, [||])
    
    let upgrade projectFile =
        let dir = FileInfo(projectFile).Directory.FullName

        let packageFile = dir + "/packages.config"

        if File.Exists(packageFile) then


            let readAllText filePath = File.ReadAllText filePath

            NewProj.Project(
                "Microsoft.NET.Sdk", 
                NewProj.PropertyGroup("net462"), 
                [| 
                    packageFile |> readAllText |> packages
                    projectFile |> readAllText |> projectReference
                |]
                )
            |> fun x -> File.WriteAllText(projectFile, x.ToString(), Text.Encoding.UTF8)


            Directory.Delete(dir + "/Properties", true)
            File.Delete(packageFile)
            if File.Exists(dir + "app.Debug.config") then File.Delete(dir + "app.Debug.config")
            if File.Exists(dir + "app.Release.config") then File.Delete(dir + "app.Release.config")
            if File.Exists(dir + "app.config") then File.Delete(dir + "app.config")

        else printfn "%s has already been processed" projectFile

    let pauseUpgrade projectFile =
        printfn "Started processing %s file" projectFile
        upgrade projectFile
        printfn "Completed processing %s file" projectFile
        printfn "<enter> to process next"
        //Console.ReadLine() |> ignore

    Directory.GetFiles("C:\Code\Engine\Services", "*.csproj", EnumerationOptions(RecurseSubdirectories = true))
    |> Seq.iter pauseUpgrade
    
    printfn "Done!"
    Console.ReadLine() |> ignore
    
    0
    