using Logging;
using Tranga.Jobs;

namespace Tranga;

public partial class Tranga : GlobalBase
{
    public bool keepRunning;
    private JobBoss _jobBoss;
    private Server server;
    
    public Tranga(Logger? logger, TrangaSettings settings) : base(logger, settings)
    {
        keepRunning = true;
        _jobBoss = new(this);
        StartJobBoss();
        this.server = new Server(this);
    }

    private void StartJobBoss()
    {
        Thread t = new (() =>
        {
            while (keepRunning)
            {
                _jobBoss.CheckJobs();
                Thread.Sleep(1000);
            }
        });
        t.Start();
    }
}