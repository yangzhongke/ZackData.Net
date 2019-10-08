using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities
{
    public class Album
    {

        public long Id { get; set; }
        public string Name_En { get; set; }
        public string Name_Chs { get; set; }
        public IList<Episode> Episodes { get; set; } = new List<Episode>();
    }
}
