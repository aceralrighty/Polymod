namespace TBD.StockPredictionModule.Load;

public static class LargeFileBatcher
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count != size)
            {
                continue;
            }

            yield return batch;
            batch = new List<T>(size);
        }
        if (batch.Count > 0)
            yield return batch;
    }
}
