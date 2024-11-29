using System.Net.Http.Json;
using System.Runtime.InteropServices;
using API.Schema.Jobs;
using log4net;
using log4net.Config;

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
            default:
                break;
        }
    }

    private void Loop()
    {
        Log.Info("Starting Loop.");
        while (!_abortReceived)
        {
            Thread.Sleep(500);
            
            IEnumerable<Worker> finishedWorkers = this._workers.Where(worker => (byte)worker.Task.Status > 4);
            foreach (Worker finishedWorker in finishedWorkers)
            {
                this._workers.Remove(finishedWorker);
                MarkJobCompleted(finishedWorker.Job);
            }
            
            Job[] workQueue = GetDueJobs();
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
    private readonly string? _apiUri = Environment.GetEnvironmentVariable("apiUri");
    private Job[] GetDueJobs()
    {
        if (_apiUri is null)
        {
            Log.Error("_apiUri is null.");
            return [];
        }
        string completeUri = MakeCompleteUri(_apiUri, DueJobsEndpoint);
        if (!Uri.TryCreate(completeUri, UriKind.Absolute, out Uri? requestUri))
        {
            Log.Error($"{completeUri} is not valid URI.");
            return [];
        }
        HttpClient client = new();

        try
        {
            HttpResponseMessage response = client.GetAsync(requestUri).Result;
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Response {response.StatusCode}");
                return [];
            }

            Job[]? jobs = response.Content.ReadFromJsonAsync<Job[]>().Result;
            Log.Debug($"Got {jobs?.Length??-1} jobs.");

            return jobs ?? [];
        }
        catch (TaskCanceledException taskCanceledException)
        {
            Log.Debug($"Request timed out. {taskCanceledException.Message}");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }

        return [];
    }

    private void MarkJobCompleted(Job job) => job.MarkCompleted();

    private static string MakeCompleteUri(string apiUri, string endpoint)
    {
        return string.Join('/',
            apiUri.EndsWith('/') ? apiUri.Substring(0, apiUri.Length - 1) : apiUri,
            endpoint.StartsWith('/') ? endpoint.Substring(1) : endpoint);
    }
}