using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DevelopingInsanity.KDM.kdacli
{
    public class CardsTableDescriptor
    {
        public string TableName { get; set; }
        public MonsterCardEntity[] Items { get; set; }

        public CardsTableDescriptor()
        { }
    }

    public class IndexTableDescriptor
    {
        public string TableName { get; set; }
        public IndexByMonsterEntity[] Items { get; set; }

        public IndexTableDescriptor()
        { }
    }

    public class MonstersTableDescriptor
    {
        public string TableName { get; set; }
        public MonsterEntry[] Items { get; set; }

        public MonstersTableDescriptor()
        { 
        }
    }

    public class DumpSerialization
    {
        public CardsTableDescriptor Cards { get; set; }
        public IndexTableDescriptor Index { get; set; }
        public MonstersTableDescriptor Monsters { get; set; }

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
