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
                    optionsBuilder.UseSqlite($"Data Source={_conn.DbHostname}");
                    break;

                case LocalConnectionProject.DbConnectionType.MySQL:
                    optionsBuilder.UseMySQL($"Server={_conn.DbHostname};Database={_conn.DbName};Uid={_conn.DbUsername};Pwd={_conn.DbPassword};");
                    break;
            }
            //optionsBuilder.UseMySql("");   
        }
    }
}
