﻿// <auto-generated />
using System;
using MessageBoard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MessageBoard.Migrations
{
    [DbContext(typeof(MessageBoardDbContext))]
    partial class MessageBoardDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("MessageBoard.Models.Complaint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AuthorId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TargetId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("TargetId");

                    b.ToTable("Complaints");
                });

            modelBuilder.Entity("MessageBoard.Models.Rateable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Rateables");

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("MessageBoard.Models.Rating", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("OwnerId")
                        .HasColumnType("int");

                    b.Property<int>("TargetId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("Value")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.HasIndex("TargetId");

                    b.ToTable("Ratings");
                });

            modelBuilder.Entity("MessageBoard.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Avatar")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("MessageBoard.Models.Post", b =>
                {
                    b.HasBaseType("MessageBoard.Models.Rateable");

                    b.Property<int?>("AuthorId")
                        .HasColumnType("int");

                    b.Property<int?>("TopicId")
                        .HasColumnType("int");

                    b.HasIndex("AuthorId");

                    b.HasIndex("TopicId");

                    b.ToTable("Posts");
                });

            modelBuilder.Entity("MessageBoard.Models.Topic", b =>
                {
                    b.HasBaseType("MessageBoard.Models.Rateable");

                    b.Property<int?>("AuthorId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasIndex("AuthorId");

                    b.ToTable("Topics");
                });

            modelBuilder.Entity("MessageBoard.Models.Complaint", b =>
                {
                    b.HasOne("MessageBoard.Models.User", "Author")
                        .WithMany("Complaints")
                        .HasForeignKey("AuthorId");

                    b.HasOne("MessageBoard.Models.Rateable", "Target")
                        .WithMany("Complaints")
                        .HasForeignKey("TargetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");

                    b.Navigation("Target");
                });

            modelBuilder.Entity("MessageBoard.Models.Rating", b =>
                {
                    b.HasOne("MessageBoard.Models.User", "Owner")
                        .WithMany("Ratings")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MessageBoard.Models.Rateable", "Target")
                        .WithMany("Ratings")
                        .HasForeignKey("TargetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");

                    b.Navigation("Target");
                });

            modelBuilder.Entity("MessageBoard.Models.Post", b =>
                {
                    b.HasOne("MessageBoard.Models.User", "Author")
                        .WithMany("Posts")
                        .HasForeignKey("AuthorId");

                    b.HasOne("MessageBoard.Models.Rateable", null)
                        .WithOne()
                        .HasForeignKey("MessageBoard.Models.Post", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MessageBoard.Models.Topic", "Topic")
                        .WithMany("Posts")
                        .HasForeignKey("TopicId");

                    b.Navigation("Author");

                    b.Navigation("Topic");
                });

            modelBuilder.Entity("MessageBoard.Models.Topic", b =>
                {
                    b.HasOne("MessageBoard.Models.User", "Author")
                        .WithMany("Topics")
                        .HasForeignKey("AuthorId");

                    b.HasOne("MessageBoard.Models.Rateable", null)
                        .WithOne()
                        .HasForeignKey("MessageBoard.Models.Topic", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");
                });

            modelBuilder.Entity("MessageBoard.Models.Rateable", b =>
                {
                    b.Navigation("Complaints");

                    b.Navigation("Ratings");
                });

            modelBuilder.Entity("MessageBoard.Models.User", b =>
                {
                    b.Navigation("Complaints");

                    b.Navigation("Posts");

                    b.Navigation("Ratings");

                    b.Navigation("Topics");
                });

            modelBuilder.Entity("MessageBoard.Models.Topic", b =>
                {
                    b.Navigation("Posts");
                });
#pragma warning restore 612, 618
        }
    }
}
