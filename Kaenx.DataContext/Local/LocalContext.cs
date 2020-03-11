using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Local
{
    public class LocalContext : DbContext
    {
        public DbSet<LocalProject> Projects { get; set; }
        public DbSet<LocalConnectionProject> ConnsProject { get; set; }
        public DbSet<LocalConnectionCatalog> ConnsCatalog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Local.db");
        }
    }
}
