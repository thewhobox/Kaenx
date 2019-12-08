using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace METS.Context.Project
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



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=" + "Project.db");
            //optionsBuilder.UseMySql("");   
        }
    }
}
