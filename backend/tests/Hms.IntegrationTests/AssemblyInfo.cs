using Xunit;

// HmsApiFactory sets a process-wide connection string before each host starts.
// Serial execution prevents parallel fixtures from overwriting each other's database.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
