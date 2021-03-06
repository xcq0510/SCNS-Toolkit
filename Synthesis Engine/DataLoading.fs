module DataLoading

open FSharp.Data

type EdgeChanges = CsvProvider<"cmpEdges.csv">

let readLines filePath = System.IO.File.ReadAllLines(filePath)

let private EDGES_FILENAME = System.Environment.GetCommandLineArgs().[2]

let geneTransitions =
    let csv = EdgeChanges.Load(EDGES_FILENAME)
    fun gene ->
        let rowsWhereGeneChanges = csv.Filter(fun row -> row.Gene = gene).Rows
        let seen = System.Collections.Generic.HashSet<string>()

        set [ for row in rowsWhereGeneChanges do
                  if not (seen.Contains row.StateB) then
                      seen.Add(row.StateA) |> ignore
                      yield (row.StateA, row.StateB) ]

let statesWithGeneTransitions =
    let csv = EdgeChanges.Load(EDGES_FILENAME)
    fun gene ->
        let rowsWhereGeneChanges = csv.Filter (fun row -> row.Gene = gene)
        set [ for r in rowsWhereGeneChanges.Rows do
                  yield r.StateA
                  yield r.StateB ]

let getExpressionProfiles (statesFilename : string) nonTransitionEnforcedStates (geneNames : string []) =
    let csv = CsvFile.Load(statesFilename).Cache()

    fun gene ->
        let statesWithGeneTransitions = statesWithGeneTransitions geneNames.[gene - 2]
        let rowsWithTransitions (row : FSharp.Data.CsvRow) = Set.contains row.Columns.[0] statesWithGeneTransitions
        let rowsWithoutTransitions (row : FSharp.Data.CsvRow) =
            (not <| Set.contains (row.Columns.[0]) statesWithGeneTransitions) &&
            (Set.contains (row.Columns.[0]) nonTransitionEnforcedStates)

        let expressionProfilesWithGeneTransitions = csv.Filter (System.Func<FSharp.Data.CsvRow,bool>(rowsWithTransitions))
        let expressionProfilesWithoutGeneTransitions = csv.Filter (System.Func<FSharp.Data.CsvRow,bool>(rowsWithoutTransitions))

        expressionProfilesWithGeneTransitions, expressionProfilesWithoutGeneTransitions

let rowToArray (row : CsvRow) =
    let dropFirstColumn = Seq.skip 1
    dropFirstColumn row.Columns |> Seq.map (System.Boolean.Parse) |> Array.ofSeq

let printEdges sq = Seq.map (sprintf "%A; ") sq |> Seq.fold (+) ""
