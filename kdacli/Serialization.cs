using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DevelopingInsanity.KDM.kdacli
{
    public class TableDescriptor
    {
        public string TableName { get; set; }
        public TableEntity[] Items { get; set; }

        public TableDescriptor()
        { }
    }

    public class DumpSerialization
    {
        public TableDescriptor[] Tables { get; set; }

        public DumpSerialization()
        { }

        public static DumpSerialization FromFile(string fileName)
        {
            string data = string.Empty;

            using (StreamReader reader = new StreamReader(File.OpenRead(fileName)))
            {
                data = reader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<DumpSerialization>(data);
        }

        public void Save(string fileName)
        {
            string data = JsonConvert.SerializeObject(this);

            using (StreamWriter writer = new StreamWriter(File.Open(fileName, FileMode.OpenOrCreate)))
            {
                writer.Write(data);
            }
        }
    }
}
