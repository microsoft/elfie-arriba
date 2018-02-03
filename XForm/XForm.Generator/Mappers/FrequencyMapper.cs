// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace XForm.Generator.Mappers
{
    public class FrequencyEntry<T>
    {
        public T Value { get; set; }
        public int RelativeFrequency { get; set; }

        public FrequencyEntry(T value, int relativeFrequency)
        {
            this.Value = value;
            this.RelativeFrequency = relativeFrequency;
        }
    }

    public class FrequencyMapper<T>
    {
        private FrequencyEntry<T>[] Options { get; set; }
        private int Total { get; set; }

        public FrequencyMapper(IList<T> options)
        {
            this.Options = new FrequencyEntry<T>[options.Count];

            for (int i = 0; i < options.Count; ++i)
            {
                this.Options[i] = new FrequencyEntry<T>(options[i], 1);
            }

            this.Total = options.Count;
        }

        public FrequencyMapper(IList<T> options, int[] frequencies)
        {
            this.Options = new FrequencyEntry<T>[options.Count];

            for (int i = 0; i < options.Count; ++i)
            {
                this.Options[i] = new FrequencyEntry<T>(options[i], frequencies[i]);
                this.Total += frequencies[i];
            }
        }


        public T Generate(uint hash)
        {
            int roll = Hashing.Extract(ref hash, this.Total);

            for (int i = 0; i < this.Options.Length - 1; ++i)
            {
                FrequencyEntry<T> option = this.Options[i];
                roll -= option.RelativeFrequency;
                if (roll <= 0) return option.Value;
            }

            return this.Options[0].Value;
        }
    }
}
