using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalConnectionProject
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public DbConnectionType Type { get; set; }
        public string DbName { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DbHostname { get; set; }


        public enum DbConnectionType
        {
            SqlLite,
            MySQL
        }

    }
}
