using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevelopingInsanity.KDM.kdaapi.DataModels
{
    public class MonsterDescriptor
    {
        private MonsterEntry _entry;

        private MonsterDescriptor(MonsterEntry entry)
        {
            _entry = entry;
        }


        public static IList<MonsterDescriptor> LoadAll()
        {
            throw new NotImplementedException();
        }

        public static MonsterDescriptor Load(string name, string level)
        {
            throw new NotImplementedException();
        }
    }
}
