using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSQueryExtractor
{
    class Program
    {
        static void Main(string[] args)
        {

            //int totalNumberOfWorkItems = 459;
            //int maxWorkItemPerQuery = 200;

            //int amountGroups = totalNumberOfWorkItems / maxWorkItemPerQuery;
            //int modulus = totalNumberOfWorkItems % maxWorkItemPerQuery;

            //List<int> breakdownList = new List<int>();
            //for (int i = 0; i < amountGroups; i++)
            //{
            //    breakdownList.Add(maxWorkItemPerQuery);
            //}

            //if (modulus > 0)
            //{
            //    breakdownList.Add(modulus);
            //}

            //Console.WriteLine("{0}: {1}", totalNumberOfWorkItems.ToString(), String.Join(", ", breakdownList));
            //Console.WriteLine(breakdownList.Count.ToString());
            //Console.ReadKey();

            ExecuteQuery newQuery = new ExecuteQuery();
            newQuery.RunGetBugsQueryUsingClientLib();
        }
    }
}
