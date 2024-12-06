using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using API.Schema.Jobs;
using log4net;
using log4net.Config;
using Newtonsoft.Json;

namespace JobWorker;

public class Monitor
{
    private bool _abortReceived = false;
    private readonly List<Worker> _workers = new();
    private static readonly ILog Log = LogManager.GetLogger(typeof(Monitor));
    
    public Monitor()
    {
        BasicConfigurator.Configure();
        PosixSignalRegistration.Create(PosixSignal.SIGINT, context => SignalReceived(context.Signal));
        PosixSignalRegistration.Create(PosixSignal.SIGHUP, context => SignalReceived(context.Signal));
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => SignalReceived(context.Signal));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context => SignalReceived(context.Signal));
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context => SignalReceived(context.Signal));
        
        Loop();
    }

    private void SignalReceived(PosixSignal signal)
    {
        Log.Info($"{Enum.GetName(signal)} received.");
        switch (signal)
        {
            case PosixSignal.SIGINT:
            case PosixSignal.SIGHUP:
            case PosixSignal.SIGTERM:
            case PosixSignal.SIGQUIT:
                _abortReceived = true;
                Log.Info("_abortReceived");
                break;
        }
    }

    private void Loop()
    {
        Log.Info("Starting Loop.");
        while (!_abortReceived)
        {
            Thread.Sleep(500);
            
            Worker[] finishedWorkers = this._workers.Where(worker => (byte)worker.Task.Status > 4).ToArray();
            foreach (Worker finishedWorker in finishedWorkers)
                MarkJobCompleted(finishedWorker);
            
            Job[] workQueue = GetDueJobs();
            if(workQueue.Length < 1)
                continue;
            Job[] distinctJobs = workQueue.DistinctBy(j => j.JobType).ToArray();
            foreach (Job distinctJob in distinctJobs)
            {
                if(_workers.Any(worker => worker.Job.JobType == distinctJob.JobType))
                    continue;
                _workers.Add(new Worker(distinctJob));
            }
        }
        Log.Info("Loop exited.");
    }

    private const string DueJobsEndpoint = "v2/Job/Due";
    private Job[] GetDueJobs()
    {
        if (MakeGetRequestApi<Job[]>(DueJobsEndpoint, out Job[]? dueJobs, [new JobJsonDeserializer()]))
            return dueJobs!;
        return [];
    }

    private const string UpdateJobStatusEndpoint = "v2/Job/{0}/Status";
    private const string CreateJobEndpoint = "v2/Job";
    private void MarkJobCompleted(Worker worker)
    {
        Log.Info($"Worker done {worker}");
        this._workers.Remove(worker);
        
        MakePatchRequestApi(string.Format(UpdateJobStatusEndpoint, worker.Job.JobId), JobState.Completed, out object? _);

        foreach (Job newJob in worker.NewJobs)
            MakePutRequestApi(CreateJobEndpoint, newJob, out object? _);
    }

    private static readonly string? APIUri = Environment.GetEnvironmentVariable("apiUri");
    
    public static bool MakeGetRequestApi<T>(string endpoint, out T? result, JsonConverter[]? converters = null) => MakeRequestApi(HttpMethod.Get, endpoint, null, out result, converters);
    public static bool MakePostRequestApi<T>(string endpoint, object? content, out T? result, JsonConverter[]? converters = null) => MakeRequestApi(HttpMethod.Post, endpoint, content, out result, converters);
    public static bool MakeDeleteRequestApi<T>(string endpoint, object? content, out T? result, JsonConverter[]? converters = null) => MakeRequestApi(HttpMethod.Delete, endpoint, content, out result, converters);
    public static bool MakePatchRequestApi<T>(string endpoint, object? content, out T? result, JsonConverter[]? converters = null) => MakeRequestApi(HttpMethod.Patch, endpoint, content, out result, converters);
    public static bool MakePutRequestApi<T>(string endpoint, object? content, out T? result, JsonConverter[]? converters = null) => MakeRequestApi(HttpMethod.Put, endpoint, content, out result, converters);
    
    public static bool MakeRequestApi<T>(HttpMethod method, string endpoint, object? content, out T? result, JsonConverter[]? converters = null)
    {
        result = default;
        if (APIUri is null)
        {
            Log.Error("_apiUri is null.");
            return false;
        }
        string completeUri = MakeCompleteUri(APIUri, endpoint);
        if (!Uri.TryCreate(completeUri, UriKind.Absolute, out Uri? requestUri))
        {
            Log.Error($"{completeUri} is not valid URI.");
            return false;
        }
        HttpClient client = new();
        HttpRequestMessage request = new (method, requestUri);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }

        try
        {
            HttpResponseMessage response = client.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"{request.RequestUri} Response {response.StatusCode}");
                return false;
            }
            
            result = JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result, converters??[]);

            return true;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Log.Debug($"Request timed out. {taskCanceledException.Message}");
        }
        catch (Exception e)
        {
            Log.Error($"Request error: {e.Message}");
        }
        return false;
    }

    private static string MakeCompleteUri(string apiUri, string endpoint)
    {
        return string.Join('/',
            apiUri.EndsWith('/') ? apiUri.Substring(0, apiUri.Length - 1) : apiUri,
            endpoint.StartsWith('/') ? endpoint.Substring(1) : endpoint);
    }
}