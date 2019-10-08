using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities
{
    public class Episode
    {
        public long Id { get; set; }
        public string Name_En { get; set; }
        public long AlbumId { get; set; }
        public Album Album { get; set; }
        public string VideoUrl { get; set; }
        public string AudioUrl { get; set; }
        public string MediaType { get; set; }        
    }
}
