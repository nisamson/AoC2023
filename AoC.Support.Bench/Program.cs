// See https://aka.ms/new-console-template for more information

using System.Reflection;
using BenchmarkDotNet.Running;

var asm = Assembly.GetCallingAssembly();
BenchmarkSwitcher.FromAssembly(asm).Run(args);