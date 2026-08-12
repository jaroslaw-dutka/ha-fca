// Integration tests share process-wide resources (the current directory for Mock files, MQTT broker
// ports), so run them serially rather than in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
