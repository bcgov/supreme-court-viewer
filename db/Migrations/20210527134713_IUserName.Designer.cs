﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Scv.Db.Models;

namespace Scv.Db.Migrations
{
    [DbContext(typeof(ScvDbContext))]
    [Migration("20210527134713_IUserName")]
    partial class IUserName
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("FriendlyName")
                        .HasColumnType("text")
                        .HasColumnName("friendly_name");

                    b.Property<string>("Xml")
                        .HasColumnType("text")
                        .HasColumnName("xml");

                    b.HasKey("Id")
                        .HasName("pk_data_protection_keys");

                    b.ToTable("data_protection_keys");
                });

            modelBuilder.Entity("Scv.Db.Models.Audit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Action")
                        .HasColumnType("text")
                        .HasColumnName("action");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created");

                    b.Property<string>("IpAddress")
                        .HasColumnType("text")
                        .HasColumnName("ip_address");

                    b.Property<string>("JsonBody")
                        .HasColumnType("jsonb")
                        .HasColumnName("json_body");

                    b.Property<string>("Path")
                        .HasColumnType("text")
                        .HasColumnName("path");

                    b.Property<string>("ResponseCode")
                        .HasColumnType("text")
                        .HasColumnName("response_code");

                    b.Property<string>("UserId")
                        .HasColumnType("text")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_audit");

                    b.ToTable("audit");
                });

            modelBuilder.Entity("Scv.Db.Models.Auth.RequestFileAccess", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("AgencyId")
                        .HasColumnType("text")
                        .HasColumnName("agency_id");

                    b.Property<DateTimeOffset>("Expires")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires");

                    b.Property<string>("FileId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("file_id");

                    b.Property<string>("PartId")
                        .HasColumnType("text")
                        .HasColumnName("part_id");

                    b.Property<DateTimeOffset>("Requested")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("requested");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("user_id");

                    b.Property<string>("UserName")
                        .HasColumnType("text")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("pk_request_file_access");

                    b.ToTable("request_file_access");
                });
#pragma warning restore 612, 618
        }
    }
}
