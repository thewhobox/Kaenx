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


        private string _connectionString;
        private DbConnectionType _connectionType;

        public CatalogContext()
        {
            _connectionString = "Data Source=" + "Catalog.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch(_connectionType)
            {
                case DbConnectionType.SqlLite:
                    optionsBuilder.UseSqlite(_connectionString);
                    break;

                case DbConnectionType.MySQL:
                    optionsBuilder.UseMySQL(_connectionString);
                    break;

            } 
        }

        public enum DbConnectionType
        {
            SqlLite,
            MySQL
        }

        public enum DbType
        {
            Local,
            Online,
            Same
        }
    }
}
