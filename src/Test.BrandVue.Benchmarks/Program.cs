using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using JetBrains.Profiler.SelfApi;
using NUnit.Framework;

namespace Test.BrandVue.Benchmarks
{
    /// <summary>
    /// Set to Release mode
    /// Set this as the startup project
    /// Press Ctrl+F5
    /// Type in 1, press enter
    /// Wait for "Press any key to close this window . . ."
    /// Look in the folder opened up
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // ReSharper disable once RedundantAssignment - see conditional compilation
            IConfig config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator); //Complains about nunit

            #if DEBUG
            Console.WriteLine("Debug mode - DO NOT USE RESULTS FROM THIS RUN - switch to release mode and rerun");
            config = new DebugInProcessConfig();
            #endif

            if (args.Length == 1 && args[0] == "DotMemory")
            {
                await MemoryProfileCalculationAsync();
            }
            else
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
            }

            #if DEBUG
            Console.WriteLine("Debug mode - DO NOT USE RESULTS FROM THIS RUN - switch to release mode and rerun");
            #endif

            Process.Start("explorer", "BenchmarkDotNet.Artifacts\\results");
        }

        /// <summary>To analyze allocations (including ones that are GC'd), run the exe from DotMemory with "DotMemory" as the parameter</summary>
        private static async Task MemoryProfileCalculationAsync()
        {

            var testRunner = new YesNoSingleResultBenchmarks();
            testRunner.Setup();
            MemoryProfiler.GetSnapshot("After setup");
            await testRunner.FieldAndPrimaryTrueValues();
            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.GetSnapshot("Warmup field calculation");

            await testRunner.FieldAndPrimaryTrueValues();
            MemoryProfiler.GetSnapshot("After actual field calculation");
            await testRunner.FieldAndPrimaryTrueValuesWithExpressionFilter();
            MemoryProfiler.GetSnapshot("After variable expression calculation");

            MemoryProfiler.Detach();
        }
    }
}
