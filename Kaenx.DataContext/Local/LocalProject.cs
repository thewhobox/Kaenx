using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalProject
    {
        [Key]
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public int ConnectionId { get; set; }
        public byte[] Thumbnail { get; set; }
        public int ThumbWidth { get; set; }
        public int ThumbHeight { get; set; }

    }
}
