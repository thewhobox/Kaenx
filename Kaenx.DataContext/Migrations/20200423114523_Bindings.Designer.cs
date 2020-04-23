﻿// <auto-generated />
using System;
using Kaenx.DataContext.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kaenx.DataContext.Migrations
{
    [DbContext(typeof(CatalogContext))]
    [Migration("20200423114523_Bindings")]
    partial class Bindings
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppAdditional", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<byte[]>("Bindings");

                    b.Property<byte[]>("ComsAll");

                    b.Property<byte[]>("ComsDefault");

                    b.Property<byte[]>("Dynamic");

                    b.Property<byte[]>("LoadProcedures");

                    b.Property<byte[]>("ParameterAll");

                    b.Property<byte[]>("ParamsHelper");

                    b.HasKey("Id");

                    b.ToTable("AppAdditionals");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppComObject", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<string>("ApplicationId")
                        .HasMaxLength(100);

                    b.Property<string>("BindedId")
                        .HasMaxLength(255);

                    b.Property<int>("Datapoint");

                    b.Property<int>("DatapointSub");

                    b.Property<bool>("Flag_Communicate");

                    b.Property<bool>("Flag_Read");

                    b.Property<bool>("Flag_ReadOnInit");

                    b.Property<bool>("Flag_Transmit");

                    b.Property<bool>("Flag_Update");

                    b.Property<bool>("Flag_Write");

                    b.Property<string>("FunctionText")
                        .HasMaxLength(100);

                    b.Property<string>("Group");

                    b.Property<int>("Number");

                    b.Property<int>("Size");

                    b.Property<string>("Text")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("AppComObjects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameter", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<int>("Access");

                    b.Property<string>("ApplicationId")
                        .HasMaxLength(100);

                    b.Property<int>("Offset");

                    b.Property<int>("OffsetBit");

                    b.Property<string>("ParameterTypeId")
                        .HasMaxLength(100);

                    b.Property<string>("SegmentId")
                        .HasMaxLength(100);

                    b.Property<int>("SegmentType");

                    b.Property<string>("SuffixText")
                        .HasMaxLength(20);

                    b.Property<string>("Text");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.ToTable("AppParameters");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameterTypeEnumViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<int>("Order");

                    b.Property<string>("ParameterId")
                        .HasMaxLength(100);

                    b.Property<string>("Text")
                        .HasMaxLength(100);

                    b.Property<string>("Value")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("AppParameterTypeEnums");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameterTypeViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<string>("ApplicationId");

                    b.Property<int>("Size");

                    b.Property<string>("Tag1")
                        .HasMaxLength(100);

                    b.Property<string>("Tag2")
                        .HasMaxLength(100);

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("AppParameterTypes");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppSegmentViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<int>("Address");

                    b.Property<string>("ApplicationId");

                    b.Property<string>("Data");

                    b.Property<int>("LsmId");

                    b.Property<string>("Mask");

                    b.Property<int>("Offset");

                    b.Property<int>("Size");

                    b.HasKey("Id");

                    b.ToTable("AppAbsoluteSegments");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.ApplicationViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<int>("Manufacturer");

                    b.Property<string>("Mask")
                        .HasMaxLength(7);

                    b.Property<string>("Name")
                        .HasMaxLength(100);

                    b.Property<int>("Number");

                    b.Property<string>("Table_Assosiations")
                        .HasMaxLength(40);

                    b.Property<int>("Table_Assosiations_Max");

                    b.Property<int>("Table_Assosiations_Offset");

                    b.Property<string>("Table_Group")
                        .HasMaxLength(40);

                    b.Property<int>("Table_Group_Max");

                    b.Property<int>("Table_Group_Offset");

                    b.Property<string>("Table_Object")
                        .HasMaxLength(40);

                    b.Property<int>("Table_Object_Offset");

                    b.Property<int>("Version");

                    b.HasKey("Id");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.CatalogViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<string>("Name")
                        .HasMaxLength(100);

                    b.Property<string>("ParentId")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.DeviceViewModel", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<int>("BusCurrent");

                    b.Property<string>("CatalogId")
                        .HasMaxLength(100);

                    b.Property<string>("HardwareId")
                        .HasMaxLength(100);

                    b.Property<bool>("HasApplicationProgram");

                    b.Property<bool>("HasIndividualAddress");

                    b.Property<bool>("IsCoupler");

                    b.Property<bool>("IsPowerSupply");

                    b.Property<bool>("IsRailMounted");

                    b.Property<string>("ManufacturerId")
                        .HasMaxLength(7);

                    b.Property<string>("Name")
                        .HasMaxLength(100);

                    b.Property<string>("OrderNumber")
                        .HasMaxLength(100);

                    b.Property<string>("VisibleDescription")
                        .HasMaxLength(300);

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.Hardware2AppModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255);

                    b.Property<string>("ApplicationId");

                    b.Property<string>("HardwareId");

                    b.Property<string>("Name")
                        .HasMaxLength(100);

                    b.Property<int>("Number");

                    b.Property<int>("Version");

                    b.HasKey("Id");

                    b.ToTable("Hardware2App");
                });
#pragma warning restore 612, 618
        }
    }
}
