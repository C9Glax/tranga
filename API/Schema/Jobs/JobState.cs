namespace API.Schema.Jobs;

public enum JobState : byte
{
    //Values 0-63 Preparation Stages
    FirstExecution = 0, 
    //64-127 Running Stages
    Running = 64,
    //128-191 Completion Stages
    Completed = 128,
    CompletedWaiting = 159,
    //192-255 Error stages
    Failed = 192
}