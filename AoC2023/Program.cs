using AdventOfCodeSupport;
using AoC2023;
using AoC2023._2023;

var solutions = new AdventSolutions();
var day = solutions.GetMostRecentDay();
// var day = solutions.GetDay(2023, 1);
await day.DownloadInputAsync();

// day.SetTestInput(day.Bag["test"]);
// await day.SubmitPart1Async();
// await day.SubmitPart2Async();
// day.Part1();
await day.CheckPart1Async();
await day.CheckPart2Async();
// if (day is IAdvent advent) {
//     // for (var i = 0; i < 100000; i++) {
//     //     advent.DoPart1();
//     // }
//     for (var i = 0; i < 100000; i++) {
//         advent.DoPart2();
//     }
// }
if (day is Day01 d1) {
    d1.PrintNumbers();
}

//
//
day.Benchmark();

// solutions.BenchmarkAll();
