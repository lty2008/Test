using System;
using System.Collections.Generic;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            InitExample();
            ShowMenu();

            AsyncLoop();
            while (true)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        static Dictionary<string, IExample> allExample = new System.Collections.Generic.Dictionary<string, IExample>();
        static void RegExample(IExample example)
        {
            allExample[example.ID.ToLower()] = example;
        }
        static void InitExample()
        {
            RegExample(new TransferOneNEO());
            // RegExample(new TransferOneNNC());
            // RegExample(new BalanceOfNNC());
        }

        static void ShowMenu()
        {
            Console.WriteLine("===all test===");
            foreach (var item in allExample)
            {
                Console.WriteLine("type '" + item.Key + "' to Run: " + item.Value.Name);
            }
            Console.WriteLine("type '?' to Get this list.");
        }

        async static void AsyncLoop()
        {
            while (true)
            {
                var line = Console.ReadLine().ToLower();
                if (line == "?" || line == "？" || line == "ls")
                {
                    ShowMenu();
                }
                else if (line == "")
                {
                    continue;
                }
                else if (allExample.ContainsKey(line))
                {
                    var example = allExample[line];
                    try
                    {
                        Console.WriteLine("[begin]" + example.Name);

                        await example.Start();

                        Console.WriteLine("[end]" + example.Name);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    Console.WriteLine("unknown line.");

                }
            }
        }
    }
}
