using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

class ForEachWithThreadLocal
{
    // Demonstrated features:
    // 		Parallel.ForEach()
    //		Thread-local state
    // Expected results:
    //      This example sums up the elements of an int[] in parallel.
    //      Each thread maintains a local sum. When a thread is initialized, that local sum is set to 0.
    //      On every iteration the current element is added to the local sum.
    //      When a thread is done, it safely adds its local sum to the global sum.
    //      After the loop is complete, the global sum is printed out.
    // Documentation:
    //		http://msdn.microsoft.com/en-us/library/dd990270(VS.100).aspx
    static void Main()
    {
        // The sum of these elements is 40.
        int[] input = { 4, 1, 6, 2, 9, 5, 10, 3 };
        int sum = 0;
         System.Timers.Timer aTimer;
    aTimer = new System.Timers.Timer(1000);

        try
        {
            var source = Enumerable.Range(1, 100000);
            

            // Opt in to PLINQ with AsParallel.
            var evenNums = from num in source.AsParallel().WithDegreeOfParallelism(8)
                           where num % 2 == 0
                           select num;
            Console.WriteLine("{0} even numbers out of {1} total",
                              evenNums.Count(), source.Count());
            // The example displays the following output:
            //       5000 even numbers out of 10000 total 
            Console.ReadLine();
        }
        // No exception is expected in this example, but if one is still thrown from a task,
        // it will be wrapped in AggregateException and propagated to the main thread.
        catch (AggregateException e)
        {
            Console.WriteLine("Parallel.ForEach has thrown an exception. THIS WAS NOT EXPECTED.\n{0}", e);
            string s = @"dfdfdf""dfdfdf""";


        }
    }

  

}