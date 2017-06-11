using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using Xsv.Sanitize;

namespace V5.ConsoleTest
{
    public class Person
    {
        private static String8Block block = new String8Block();
        private static PersonNameMapper mapper = new PersonNameMapper();
        private static String8 Citizen = String8.Convert("Citizen", new byte[7]);
        private static String8 Male = String8.Convert("Male", new byte[4]);
        private static String8 Female = String8.Convert("Female", new byte[6]);

        public PersonName Name;
        public String8 Gender;
        public DateTime BirthDate;
        public DateTime WhenAdded;
        public int ZipCode;

        public Person(Random r)
        {
            // Random name; 2K unique first names, 8k last names, 256 middle names
            //this.Name = mapper.GenerateName(unchecked((uint)r.Next()));

            // 50% Male / Female
            this.Gender = (r.NextDouble() > 0.50 ? Female : Male);

            // Date only, evenly in the last 100 years
            this.BirthDate = (DateTime.Today.Add(TimeSpan.FromDays(-100 * 365 * r.NextDouble()))).Date;

            // DateTime, evenly in the last 3 years
            this.WhenAdded = (DateTime.Today.Add(TimeSpan.FromDays(-3 * 365 * r.NextDouble())));

            // Random 5 digit integer
            this.ZipCode = r.Next(99999);
        }

        public void Write(ITabularWriter w)
        {
            if(w.RowCountWritten == 0) w.SetColumns(new string[] { "FirstName", "MiddleName", "LastName", "Citizenship", "Gender", "BirthDate", "WhenAdded", "ZipCode" });

            w.Write(block.GetCopy(Name.FirstName));
            w.Write(block.GetCopy(Name.MiddleName));
            w.Write(block.GetCopy(Name.LastName));
            w.Write(Citizen);

            w.Write(this.Gender);
            w.Write(this.BirthDate);
            w.Write(this.WhenAdded);
            w.Write(this.ZipCode);

            w.NextRow();
            block.Clear();
        }
    }

    public class GeneratePeopleSample
    {
        public static void Generate(string outputPath, long count, int seed = 0)
        {
            using (ITabularWriter writer = TabularFactory.BuildWriter(outputPath))
            {
                Random r = new Random(seed);

                for (long i = 0; i < count; ++i)
                {
                    Person p = new Person(r);
                    p.Write(writer);
                }
            }
        }

        //public static PersonDatabase Import(string inputPath, long count)
        //{
        //    PersonDatabase db = new PersonDatabase(count);

        //    using (new TraceWatch($"Building Database of {inputPath}..."))
        //    {
        //        using (ITabularReader reader = TabularFactory.BuildReader(inputPath))
        //        {
        //            int birthDateIndex = reader.ColumnIndex("BirthDate");
        //            int whenAddedIndex = reader.ColumnIndex("WhenAdded");
        //            int zipCodeIndex = reader.ColumnIndex("ZipCode");

        //            int i = 0;
        //            while (reader.NextRow())
        //            {
        //                db.BirthDate[i] = reader.Current(birthDateIndex).ToDateTime();
        //                db.WhenAdded[i] = reader.Current(whenAddedIndex).ToDateTime();
        //                db.ZipCode[i] = reader.Current(zipCodeIndex).ToInteger();

        //                i++;
        //            }
        //        }
        //    }

        //    return db;
        //}
    }
}
