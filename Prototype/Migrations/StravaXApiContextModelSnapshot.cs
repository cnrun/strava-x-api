﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Prototype.Model;

namespace Prototype.Migrations
{
    [DbContext(typeof(StravaXApiContext))]
    partial class StravaXApiContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.0.0");

            modelBuilder.Entity("Prototype.Model.ActivityShort", b =>
                {
                    b.Property<string>("ActivityId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ActivityDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("ActivityImageMapUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("ActivityTitle")
                        .HasColumnType("TEXT");

                    b.Property<int>("ActivityType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AthleteId")
                        .HasColumnType("TEXT");

                    b.HasKey("ActivityId");

                    b.ToTable("ActivityShortDB");
                });
#pragma warning restore 612, 618
        }
    }
}
