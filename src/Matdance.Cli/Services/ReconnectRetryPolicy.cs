namespace Matdance.Cli.Services;

public static class ReconnectRetryPolicy
{
    private static readonly int[] RetryBatches = { 10, 10 };
    public static int BatchCount => RetryBatches.Length;
    public static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(3);

    public static int TotalRetries => RetryBatches.Sum();

    public static int TotalAttempts => TotalRetries + 1;

    public static ReconnectRetryStep? GetStepAfterFailure(int failedAttempt)
    {
        if (failedAttempt <= 0 || failedAttempt > TotalRetries)
            return null;

        var remainingRetry = failedAttempt;
        for (var batch = 1; batch <= BatchCount; batch++)
        {
            var attemptsInBatch = AttemptsInBatch(batch);
            if (remainingRetry <= attemptsInBatch)
            {
                return new ReconnectRetryStep(
                    RetryNumber: failedAttempt,
                    TotalRetries: TotalRetries,
                    Batch: batch,
                    TotalBatches: BatchCount,
                    AttemptInBatch: remainingRetry,
                    AttemptsInBatch: attemptsInBatch,
                    Delay: ProbeInterval,
                    BatchBudget: TimeSpan.FromSeconds(attemptsInBatch * ProbeInterval.TotalSeconds));
            }

            remainingRetry -= attemptsInBatch;
        }

        return null;
    }

    public static string Describe(ReconnectRetryStep step)
    {
        return $"Retry batch {step.Batch}/{step.TotalBatches}, probe {step.AttemptInBatch}/{step.AttemptsInBatch}; retrying in {step.Delay.TotalSeconds:F0}s (batch budget {step.BatchBudget.TotalSeconds:F0}s, total retry budget {TotalRetries} probes).";
    }

    private static int AttemptsInBatch(int batch)
    {
        return RetryBatches[Math.Clamp(batch - 1, 0, RetryBatches.Length - 1)];
    }
}

public sealed record ReconnectRetryStep(
    int RetryNumber,
    int TotalRetries,
    int Batch,
    int TotalBatches,
    int AttemptInBatch,
    int AttemptsInBatch,
    TimeSpan Delay,
    TimeSpan BatchBudget);
