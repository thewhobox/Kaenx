﻿// <auto-generated />
using System;
using Kaenx.DataContext.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kaenx.DataContext.Migrations.Project
{
    [DbContext(typeof(ProjectContext))]
    [Migration("20200427083623_RemovedImgSize")]
    partial class RemovedImgSize
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("Kaenx.DataContext.Project.ChangeParamModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DeviceId");

                    b.Property<string>("ParamId");

                    b.Property<int>("StateId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.ToTable("ChangesParam");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.ComObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ComId");

                    b.Property<int>("DeviceId");

                    b.Property<string>("Groups");

                    b.HasKey("Id");

                    b.ToTable("ComObjects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.GroupAddressModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Id");

                    b.Property<string>("Name");

                    b.Property<int>("ParentId");

                    b.Property<int>("ProjectId");

                    b.HasKey("UId");

                    b.ToTable("GroupAddress");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.GroupMainModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Id");

                    b.Property<string>("Name");

                    b.Property<int>("ProjectId");

                    b.HasKey("UId");

                    b.ToTable("GroupMain");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.GroupMiddleModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Id");

                    b.Property<string>("Name");

                    b.Property<int>("ParentId");

                    b.Property<int>("ProjectId");

                    b.HasKey("UId");

                    b.ToTable("GroupMiddle");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineDeviceModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationId");

                    b.Property<string>("DeviceId");

                    b.Property<int>("Id");

                    b.Property<bool>("LoadedApp");

                    b.Property<bool>("LoadedGA");

                    b.Property<bool>("LoadedPA");

                    b.Property<string>("Name");

                    b.Property<int>("ParentId");

                    b.Property<int>("ProjectId");

                    b.Property<byte[]>("Serial");

                    b.HasKey("UId");

                    b.ToTable("LineDevices");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineMiddleModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Id");

                    b.Property<bool>("IsExpanded");

                    b.Property<string>("Name");

                    b.Property<int>("ParentId");

                    b.Property<int>("ProjectId");

                    b.HasKey("UId");

                    b.ToTable("LinesMiddle");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Id");

                    b.Property<bool>("IsExpanded");

                    b.Property<string>("Name");

                    b.Property<int>("ProjectId");

                    b.HasKey("UId");

                    b.ToTable("LinesMain");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.ProjectModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Image");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.StateModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description")
                        .HasMaxLength(200);

                    b.Property<string>("Name")
                        .HasMaxLength(50);

                    b.Property<int>("ProjectId");

                    b.HasKey("Id");

                    b.ToTable("States");
                });
#pragma warning restore 612, 618
        }
    }
}
