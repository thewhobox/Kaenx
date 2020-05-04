using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.DataContext.Project
{
    public class ProjectContext : DbContext
    {
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<StateModel> States { get; set; }
        public DbSet<ChangeParamModel> ChangesParam { get; set; }

        public DbSet<LineModel> LinesMain { get; set; }
        public DbSet<LineMiddleModel> LinesMiddle { get; set; }
        public DbSet<LineDeviceModel> LineDevices { get; set; }
        public DbSet<ComObject> ComObjects { get; set; }
        public DbSet<GroupMainModel> GroupMain { get; set; }
        public DbSet<GroupMiddleModel> GroupMiddle { get; set; }
        public DbSet<GroupAddressModel> GroupAddress { get; set; }


        private LocalConnectionProject _conn;

        public ProjectContext()
        {
            _conn = new LocalConnectionProject() { DbHostname = "Projects.sb", Type = LocalConnectionProject.DbConnectionType.SqlLite };
        }
        public ProjectContext(LocalConnectionProject conn) => _conn = conn;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_conn.Type)
            {
                case LocalConnectionProject.DbConnectionType.SqlLite:
                    if (string.IsNullOrEmpty(_conn.DbPassword))
                    {
                        optionsBuilder.UseSqlite("Data Source=" + _conn.DbHostname);
                    } else
                    {
                        var conn = new System.Data.SQLite.SQLiteConnection(@"Data Source=C:\Users\mikeg\AppData\Local\Packages\55505Exe-Creation.KAENX_zkfs9vqf4wsm0\LocalState\Projetcs.db;");
                        conn.Open();

                        var command = conn.CreateCommand();
                        command.CommandText = $"PRAGMA key = {_conn.DbPassword};";
                        command.ExecuteNonQuery();

                        optionsBuilder.UseSqlite(conn);
                    }
                    break;

                case LocalConnectionProject.DbConnectionType.MySQL:
                    optionsBuilder.UseMySql($"Server={_conn.DbHostname};Database={_conn.DbName};Uid={_conn.DbUsername};Pwd={_conn.DbPassword};");
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChangeParamModel>().Property(p => p.Id).HasComputedColumnSql("Id");
        }
    }
}
