using AdventOfCodeSupport;
using AoC2023;

var solutions = new AdventSolutions();
var day = solutions.GetMostRecentDay();
await day.DownloadInputAsync();

// day.SetTestInput(day.Bag["example"]);
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

//
//
day.Benchmark();

// solutions.BenchmarkAll();
