using System;
using System.Collections.Generic;
using System.Text;
using V5.Collections;

namespace V5.Extensions
{
    public static class ArrayExtensions
    {
        public static long[] ToPrimitiveArray(this DateTime[] values)
        {
            long[] result = new long[values.Length];

            for (int i = 0; i < values.Length; ++i)
            {
                result[i] = values[i].ToUniversalTime().Ticks;
            }

            return result;
        }

        public static long[] ToPrimitiveArray(this TimeSpan[] values)
        {
            long[] result = new long[values.Length];

            for (int i = 0; i < values.Length; ++i)
            {
                result[i] = values[i].Ticks;
            }

            return result;
        }

        public static DateTime[] ToDateTimeArray(this long[] utcTicksArray)
        {
            DateTime[] result = new DateTime[utcTicksArray.Length];

            for (int i = 0; i < utcTicksArray.Length; ++i)
            {
                result[i] = new DateTime(utcTicksArray[i], DateTimeKind.Utc);
            }

            return result;
        }

        public static TimeSpan[] ToTimeSpanArray(this long[] ticksArray)
        {
            TimeSpan[] result = new TimeSpan[ticksArray.Length];

            for (int i = 0; i < ticksArray.Length; ++i)
            {
                result[i] = new TimeSpan(ticksArray[i]);
            }

            return result;
        }

        public static T[] Sample<T>(this T[] values, int countToSample, Random r)
        {
            T[] sample;

            // If there aren't enough items, return a copy of all of them
            if (countToSample >= values.Length)
            {
                sample = new T[values.Length];
                values.CopyTo(sample, 0);
                return sample;
            }

            sample = new T[countToSample];
            int samplesAdded = 0;

            if (countToSample * 4 < values.Length)
            {
                // If there are many more values than samples, choose indices randomly which aren't yet included
                IndexSet set = new IndexSet(values.Length);
                while (samplesAdded < countToSample)
                {
                    int sourceIndex = r.Next(0, values.Length);
                    if (set[sourceIndex]) continue;

                    sample[samplesAdded++] = values[sourceIndex];
                    set[sourceIndex] = true;
                }
            }
            else
            {
                // Otherwise, go through each item calculating the probability it should be included
                for (int i = 0; i < values.Length && samplesAdded < countToSample; ++i)
                {
                    // Calculate a threshold to include this item.
                    // Basically, int.MaxValue * (samplesStillNeeded) / (itemsStillAvailable)
                    int includeThresholdPerItem = int.MaxValue / (values.Length - i);
                    int includeThreshold = includeThresholdPerItem * (countToSample - samplesAdded);

                    // If the next random value meets the threshold, include the item
                    int includeChance = r.Next();
                    if (includeChance < includeThreshold)
                    {
                        sample[samplesAdded++] = values[i];
                    }
                }
            }

            return sample;
        }
    }
}
