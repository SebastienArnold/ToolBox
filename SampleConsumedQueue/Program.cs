using As.Toolbox.Threading;
using System;
using System.Globalization;
using System.Threading;

namespace SampleConsumedQueue
{
    class Program
    {
        private static void Main(string[] args)
        {           
            // First demo : Dequeue soon as possible.

            Console.WriteLine("");
            Console.WriteLine("Just enqueue and let dequeue soon as possible.");
            Console.WriteLine("");

            var queue = new ConsumedQueue<string>(3, s =>
            {
                Console.WriteLine("(-) Consume {0}", s);
                Thread.Sleep(3500);
            });

            FillQueue(queue);

            // Wait all process terminated before 2nd demo...
            while (queue.ItemsCount > 0)
            {
                Thread.Sleep(100);
            }

            // Second demo : Use pause / reusme.

            Console.WriteLine("");
            Console.WriteLine("And now with pause / resume ...");
            Console.WriteLine("");

            Console.WriteLine("Pause queue and fill...");
            queue.Pause();

            FillQueue(queue);

            Console.WriteLine("Resume the queue...");
            queue.Resume();

            // Finish, wait any key to close.
            Console.ReadLine();
        }

        private static void FillQueue(ConsumedQueue<string> queue)
        {
            for (var i=1; i<10; i++)
            {
                Console.WriteLine("(+) Enqueue {0}", i);
                queue.EnqueueItem(i.ToString(CultureInfo.InvariantCulture));
                Thread.Sleep(500);
            }
        }
    }
}
