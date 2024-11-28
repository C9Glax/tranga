using System.Runtime.InteropServices;
using API.Schema.Jobs;

namespace JobWorker;

public class Monitor
{
    private bool _abortReceived = false;
    private readonly Thread _runner;
    private readonly List<Worker> _workers = new();
    
    public Monitor()
    {
        PosixSignalRegistration.Create(PosixSignal.SIGINT, context => _abortReceived = true);
        PosixSignalRegistration.Create(PosixSignal.SIGHUP, context => _abortReceived = true);
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => _abortReceived = true);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context => _abortReceived = true);
        _runner = new Thread(Loop);
        _runner.Start();
    }

    private void Loop()
    {
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
    }

    private Job[] GetDueJobs()
    {
        
    }

    private void MarkJobCompleted(Job job) => job.MarkCompleted();
}