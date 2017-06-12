
public static T[] EytzingerSort(T[] array)
{
    int outCount = 0;
    T[] output = new T[array.Length];

    Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
    queue.Enqueue(Tuple.Create(0, array.Length - 1));

    while (queue.Count > 0)
    {
        Tuple<int, int> value = queue.Dequeue();
        int index = (value.Item1 + value.Item2) / 2;
        output[outCount++] = array[index];

        if (value.Item1 < index)
        {
            queue.Enqueue(Tuple.Create(value.Item1, index - 1));
        }

        if (value.Item2 > index)
        {
            queue.Enqueue(Tuple.Create(index + 1, value.Item2));
        }
    }

    return output;
}