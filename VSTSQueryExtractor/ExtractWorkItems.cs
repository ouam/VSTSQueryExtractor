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
using System.Collections;
using Microsoft.TeamFoundation.Core.WebApi;
using System.IO;

public class Bug
{
    public string ID { get; set; }
    public string Title { get; set; }
    public string State { get; set; }
}

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
        // url link on how to renew token: docs.microsoft.com/en-us/vsts/git/_shared/personal-access-tokens
        //_personalAccessToken = "yyvcygnult4ag7njpp2updqc6rqmx7h3lanzfpim4ccnceg6js5a";    original token now expired
        _personalAccessToken = "mta2r46jsdr3k6gtaprhalcofdrzasrt3rxuktfepghkcmmzanya";      //new token. Expires on the 3rd of March 2019
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
            //Query all bugs from all projects from 01/10/2017 (bug 10187)
            Query = "Select *" +
                    "From WorkItems " +
                    "Where [Work Item Type] = 'Bug' " +
                    "And [System.Id] > '10186' " +
                    "Order By [State] Asc, [Changed Date] Desc"

            //Query all active bugs from 1 project (specified above)
            //Query = "Select * " +
            //        "From WorkItems " +
            //        "Where [Work Item Type] = 'Bug' " +
            //        "And [System.TeamProject] = '" + project + "' " +
            //        "And [System.State] <> 'Closed' " +
            //        "And [System.State] <> 'Backlog' " +
            //        "Order By [State] Asc, [Changed Date] Desc"
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
                string[] fields = new string[] {
                    "System.Id",
                    "System.Title",
                    "System.State",
                    "System.Tags",
                    "System.AreaPath",
                    "AutologicKanban.Projectnamenew",
                    "AutologicKanban.Projectversion",
                    "AutologicKanban.Defectorigin",
                    "AutologicKanban.Defectdetection",
                    "AutologicKanban.Defectrootcause",
                    "AutologicKanban.Testernew",
                    "AutologicKanban.Developernew",
                    "System.CreatedDate",
                    "Microsoft.VSTS.Common.ClosedDate"
                };

                List<WorkItem> fullWorkItems = new List<WorkItem>();

                for (int i = 0; i < breakdownList.Count; i++)
                {
                    int[] subarr = new int[breakdownList[i]];
                    Array.Copy(arr, i * maxWorkItemPerQuery, subarr, 0, breakdownList[i]);

                    //get work items for the ids found in query
                    var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(subarr, fields, workItemQueryResult.AsOf).Result;

                    fullWorkItems.AddRange(workItems);
                }

                //Console.WriteLine(String.Join(",", fields));

                //create the overall dictionary of bugs
                Dictionary<string, Dictionary<string, string>> totalbugs = new Dictionary<string, Dictionary<string, string>>();

                //loop though work items and write to console
                foreach (var workItem in fullWorkItems)
                {

                    Dictionary<string, string> bug = new Dictionary<string, string>();

                    foreach (var field in workItem.Fields)
                    {
                        try
                        {
                            bug.Add(field.Key.Split('.').Last(), field.Value.ToString());
                        }
                        catch
                        {
                            Console.WriteLine("Could not add {0} : {1} from work item {2}!", field.Key.Split('.').Last(), field.Value.ToString(), workItem.Id);
                        }
                    }

                    //display bug dictionary as json format
                    //string json_bug = JsonConvert.SerializeObject(bug, Formatting.Indented);
                    //Console.WriteLine(json_bug);

                    //append bug to dictionary containing all bugs
                    totalbugs.Add(workItem.Id.ToString(), bug);
                }

                //archive the existing bugs_data json file before creating the new one
                string archiveFilename = "bugs_data_" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + ".json";
                File.Copy(Path.Combine(@"W:\Test_Team\", "bugs_data.json"), Path.Combine(@"W:\Test_Team\", archiveFilename), true);

                //create json file containing all bugs from the query
                string jsonTotalbugs = JsonConvert.SerializeObject(totalbugs, Formatting.Indented);
                System.IO.File.WriteAllText(@"W:\Test_Team\bugs_data.json", jsonTotalbugs);

                //Console.ReadKey();
                return fullWorkItems;

            }

            return null;
        }
    }

}