using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Kaenx.DataContext.Catalog
{
    public class CatalogContext : DbContext
    {
        public DbSet<DeviceViewModel> Devices { get; set; }
        public DbSet<ApplicationViewModel> Applications { get; set; }
        public DbSet<CatalogViewModel> Sections { get; set; }

        public DbSet<AppComObject> AppComObjects { get; set; }
        public DbSet<AppParameter> AppParameters { get; set; }
        public DbSet<AppAdditional> AppAdditionals { get; set; }
        public DbSet<AppAbsoluteSegmentViewModel> AppAbsoluteSegments { get; set; }
        public DbSet<AppParameterTypeViewModel> AppParameterTypes { get; set; }
        public DbSet<AppParameterTypeEnumViewModel> AppParameterTypeEnums { get; set; }
        public DbSet<Hardware2AppModel> Hardware2App { get; set; }


        private LocalConnectionCatalog _conn;

        public CatalogContext()
        {
            //_connectionString = "Data Source=" + "Catalog.db";
            _conn = new LocalConnectionCatalog() { DbHostname = "Catalog.db", Type = LocalConnectionCatalog.DbConnectionType.SqlLite };
        }
        public CatalogContext(LocalConnectionCatalog conn) => _conn = conn;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_conn.Type)
            {
                case LocalConnectionCatalog.DbConnectionType.SqlLite:
                    optionsBuilder.UseSqlite($"Data Source={_conn.DbHostname}");
                    break;

                case LocalConnectionCatalog.DbConnectionType.MySQL:
                    optionsBuilder.UseMySQL($"Server={_conn.DbHostname};Database={_conn.DbName};Uid={_conn.DbUsername};Pwd={_conn.DbPassword};");
                    break;
            }  
        }
    }
}
