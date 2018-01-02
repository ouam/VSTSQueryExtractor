using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;

public class ExecuteQuery
{
    readonly string _uri;
    readonly string _personalAccessToken;
    readonly string _project;


    /// <summary>
    /// Constructor. Manually set values to match your account.
    /// </summary>
    public ExecuteQuery()
    {
        _uri = "https://autologic.visualstudio.com";
        _personalAccessToken = "yyvcygnult4ag7njpp2updqc6rqmx7h3lanzfpim4ccnceg6js5a";
        _project = "Kanban";
    }

    /// <summary>
    /// Execute a WIQL query to return a list of bugs using the .NET client library
    /// </summary>
    /// <returns>List of Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem</returns>
    public List<WorkItem> RunGetBugsQueryUsingClientLib()
    {
        Uri uri = new Uri(_uri);
        string personalAccessToken = _personalAccessToken;
        string project = _project;

        VssBasicCredential credentials = new VssBasicCredential("", _personalAccessToken);

        //create a wiql object and build our query
        Wiql wiql = new Wiql()
        {
            //Query all bugs from all projects
            //Query = "Select [System.Id],[System.Title],[System.State],[System.AreaPath],[AutologicKanban.Projectnamenew],[AutologicKanban.Projectversion],[System.CreatedDate],[AutologicKanban.Defectdetection],[AutologicKanban.Defectorigin],[AutologicKanban.Defectrootcause],[AutologicKanban.Tester] " +
            //        "From WorkItems " +
            //        "Where [Work Item Type] = 'Bug' " +
            //        "Order By [State] Asc, [Changed Date] Desc"

            //Query all active bugs from 1 project (specified above)
            Query = "Select * " +
                    "From WorkItems " +
                    "Where [Work Item Type] = 'Bug' " +
                    "And [System.TeamProject] = '" + project + "' " +
                    "And [System.State] <> 'Closed' " +
                    "And [System.State] <> 'Backlog' " +
                    "Order By [State] Asc, [Changed Date] Desc"
        };


        //create instance of work item tracking http client
        using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(uri, credentials))
        {
            //execute the query to get the list of work items in the results
            WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByWiqlAsync(wiql).Result;

            //some error handling                
            if (workItemQueryResult.WorkItems.Count() != 0)
            {

                //need to get the list of our work item ids, divide them in groups of 200 if necessary and put them into an array
                int totalNumberOfWorkItems = workItemQueryResult.WorkItems.Count();
                int maxWorkItemPerQuery = 200;

                int amountGroups = totalNumberOfWorkItems / maxWorkItemPerQuery;
                int modulus = totalNumberOfWorkItems % maxWorkItemPerQuery;

                List<int> breakdownList = new List<int>();
                for (int i = 0; i < amountGroups; i++)
                {
                    breakdownList.Add(maxWorkItemPerQuery);
                }

                if (modulus > 0)
                {
                    breakdownList.Add(modulus);
                }

                List<int> list = new List<int>();
                foreach (var item in workItemQueryResult.WorkItems)
                {
                    list.Add(item.Id);
                }
                int[] arr = list.ToArray();

                //build a list of the fields we want to see
                string[] fields = new string[5];
                fields[0] = "System.Id";
                fields[1] = "System.Title";
                fields[2] = "System.State";
                fields[3] = "System.AreaPath";
                fields[4] = "System.CreatedDate";
                //fields[5] = "AutologicKanban.ClosedDate";
                //fields[6] = "AutologicKanban.Projectnamenew";
                //fields[7] = "AutologicKanban.Projectversion";
                //fields[8] = "AutologicKanban.Defectdetection";
                //fields[9] = "AutologicKanban.Defectorigin";
                //fields[10] = "AutologicKanban.Defectrootcause";
                //fields[11] = "AutologicKanban.Tester";


                List<WorkItem> fullWorkItems = new List<WorkItem>();

                for (int i = 0; i < breakdownList.Count; i++)
                {
                    int[] subarr = new int[breakdownList[i]];
                    Array.Copy(arr, i * maxWorkItemPerQuery, subarr, 0, breakdownList[i]);

                    //get work items for the ids found in query
                    var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(subarr, fields, workItemQueryResult.AsOf).Result;

                    fullWorkItems.AddRange(workItems);
                }

                //loop though work items and write to console
                foreach (var workItem in fullWorkItems)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4}", workItem.Id, workItem.Fields["System.Title"], workItem.Fields["System.State"], workItem.Fields["System.AreaPath"], workItem.Fields["System.CreatedDate"]);
                    //Console.WriteLine("{0},{1},{2},{3},{4},{5}", workItem.Id, workItem.Fields["System.Title"], workItem.Fields["System.State"], workItem.Fields["System.AreaPath"], workItem.Fields["System.CreatedDate"], workItem.Fields["AutologicKanban.ClosedDate"]);
                    //Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", workItem.Id, workItem.Fields["System.Title"], workItem.Fields["System.State"], workItem.Fields["System.AreaPath"], workItem.Fields["System.CreatedDate"], workItem.Fields["AutologicKanban.ClosedDate"], workItem.Fields["AutologicKanban.Projectnamenew"], workItem.Fields["AutologicKanban.Projectversion"], workItem.Fields["AutologicKanban.Defectdetection"], workItem.Fields["AutologicKanban.Defectorigin"], workItem.Fields["AutologicKanban.Defectrootcause"], workItem.Fields["AutologicKanban.Tester"]);
                }

                Console.ReadKey();
                return fullWorkItems;

            }

            return null;
        }
    }

}