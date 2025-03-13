﻿// <auto-generated />
using System;
using System.Collections.Generic;
using API.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    [DbContext(typeof(PgsqlContext))]
    [Migration("20250313180926_dev-130325-4")]
    partial class dev1303254
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "hstore");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Schema.Author", b =>
                {
                    b.Property<string>("AuthorId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("AuthorName")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.HasKey("AuthorId");

                    b.ToTable("Authors");
                });

            modelBuilder.Entity("API.Schema.Chapter", b =>
                {
                    b.Property<string>("ChapterId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("ArchiveFileName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ChapterNumber")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<bool>("Downloaded")
                        .HasColumnType("boolean");

                    b.Property<string>("ParentMangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Title")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<int?>("VolumeNumber")
                        .HasColumnType("integer");

                    b.HasKey("ChapterId");

                    b.HasIndex("ParentMangaId");

                    b.ToTable("Chapters");
                });

            modelBuilder.Entity("API.Schema.Jobs.Job", b =>
                {
                    b.Property<string>("JobId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.PrimitiveCollection<string[]>("DependsOnJobsIds")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("text[]");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean");

                    b.Property<string>("JobId1")
                        .HasColumnType("character varying(64)");

                    b.Property<byte>("JobType")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("LastExecution")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ParentJobId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<decimal>("RecurrenceMs")
                        .HasColumnType("numeric(20,0)");

                    b.Property<byte>("state")
                        .HasColumnType("smallint");

                    b.HasKey("JobId");

                    b.HasIndex("JobId1");

                    b.HasIndex("ParentJobId");

                    b.ToTable("Jobs");

                    b.HasDiscriminator<byte>("JobType");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("API.Schema.LibraryConnectors.LibraryConnector", b =>
                {
                    b.Property<string>("LibraryConnectorId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Auth")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("BaseUrl")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<byte>("LibraryType")
                        .HasColumnType("smallint");

                    b.HasKey("LibraryConnectorId");

                    b.ToTable("LibraryConnectors");

                    b.HasDiscriminator<byte>("LibraryType");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("API.Schema.Link", b =>
                {
                    b.Property<string>("LinkId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("LinkProvider")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.HasKey("LinkId");

                    b.HasIndex("MangaId");

                    b.ToTable("Link");
                });

            modelBuilder.Entity("API.Schema.Manga", b =>
                {
                    b.Property<string>("MangaId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("CoverFileNameInCache")
                        .HasColumnType("text");

                    b.Property<string>("CoverUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FolderName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IdOnConnectorSite")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<float>("IgnoreChapterBefore")
                        .HasColumnType("real");

                    b.Property<string>("MangaConnectorId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("OriginalLanguage")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<byte>("ReleaseStatus")
                        .HasColumnType("smallint");

                    b.Property<string>("WebsiteUrl")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<long>("Year")
                        .HasColumnType("bigint");

                    b.HasKey("MangaId");

                    b.HasIndex("MangaConnectorId");

                    b.ToTable("Manga");
                });

            modelBuilder.Entity("API.Schema.MangaAltTitle", b =>
                {
                    b.Property<string>("AltTitleId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("AltTitleId");

                    b.HasIndex("MangaId");

                    b.ToTable("AltTitles");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.MangaConnector", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.PrimitiveCollection<string[]>("BaseUris")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("text[]");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean");

                    b.Property<string>("IconUrl")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.PrimitiveCollection<string[]>("SupportedLanguages")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("text[]");

                    b.HasKey("Name");

                    b.ToTable("MangaConnectors");

                    b.HasDiscriminator<string>("Name").HasValue("MangaConnector");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("API.Schema.MangaTag", b =>
                {
                    b.Property<string>("Tag")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasKey("Tag");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("API.Schema.Notification", b =>
                {
                    b.Property<string>("NotificationId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<byte>("Urgency")
                        .HasColumnType("smallint");

                    b.HasKey("NotificationId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("API.Schema.NotificationConnectors.NotificationConnector", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<Dictionary<string, string>>("Headers")
                        .IsRequired()
                        .HasColumnType("hstore");

                    b.Property<string>("HttpMethod")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Name");

                    b.ToTable("NotificationConnectors");
                });

            modelBuilder.Entity("AuthorManga", b =>
                {
                    b.Property<string>("AuthorsAuthorId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.HasKey("AuthorsAuthorId", "MangaId");

                    b.HasIndex("MangaId");

                    b.ToTable("AuthorManga");
                });

            modelBuilder.Entity("MangaMangaTag", b =>
                {
                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("MangaTagsTag")
                        .HasColumnType("character varying(64)");

                    b.HasKey("MangaId", "MangaTagsTag");

                    b.HasIndex("MangaTagsTag");

                    b.ToTable("MangaMangaTag");
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadAvailableChaptersJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("MangaId")
                                .HasColumnName("DownloadAvailableChaptersJob_MangaId");
                        });

                    b.HasDiscriminator().HasValue((byte)1);
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadMangaCoverJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.HasDiscriminator().HasValue((byte)4);
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadSingleChapterJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("ChapterId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("ChapterId");

                    b.HasDiscriminator().HasValue((byte)0);
                });

            modelBuilder.Entity("API.Schema.Jobs.MoveFileOrFolderJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("FromLocation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ToLocation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue((byte)3);
                });

            modelBuilder.Entity("API.Schema.Jobs.RetrieveChaptersJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("MangaId")
                                .HasColumnName("RetrieveChaptersJob_MangaId");
                        });

                    b.HasDiscriminator().HasValue((byte)5);
                });

            modelBuilder.Entity("API.Schema.Jobs.UpdateFilesDownloadedJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("MangaId")
                                .HasColumnName("UpdateFilesDownloadedJob_MangaId");
                        });

                    b.HasDiscriminator().HasValue((byte)6);
                });

            modelBuilder.Entity("API.Schema.Jobs.UpdateMetadataJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("MangaId")
                                .HasColumnName("UpdateMetadataJob_MangaId");
                        });

                    b.HasDiscriminator().HasValue((byte)2);
                });

            modelBuilder.Entity("API.Schema.LibraryConnectors.Kavita", b =>
                {
                    b.HasBaseType("API.Schema.LibraryConnectors.LibraryConnector");

                    b.HasDiscriminator().HasValue((byte)1);
                });

            modelBuilder.Entity("API.Schema.LibraryConnectors.Komga", b =>
                {
                    b.HasBaseType("API.Schema.LibraryConnectors.LibraryConnector");

                    b.HasDiscriminator().HasValue((byte)0);
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.AsuraToon", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("AsuraToon");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.Bato", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("Bato");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.MangaDex", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("MangaDex");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.MangaHere", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("MangaHere");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.MangaKatana", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("MangaKatana");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.Manganato", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("Manganato");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.Mangaworld", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("Mangaworld");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.ManhuaPlus", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("ManhuaPlus");
                });

            modelBuilder.Entity("API.Schema.MangaConnectors.Weebcentral", b =>
                {
                    b.HasBaseType("API.Schema.MangaConnectors.MangaConnector");

                    b.HasDiscriminator().HasValue("Weebcentral");
                });

            modelBuilder.Entity("API.Schema.Chapter", b =>
                {
                    b.HasOne("API.Schema.Manga", "ParentManga")
                        .WithMany()
                        .HasForeignKey("ParentMangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentManga");
                });

            modelBuilder.Entity("API.Schema.Jobs.Job", b =>
                {
                    b.HasOne("API.Schema.Jobs.Job", null)
                        .WithMany("DependsOnJobs")
                        .HasForeignKey("JobId1");

                    b.HasOne("API.Schema.Jobs.Job", "ParentJob")
                        .WithMany()
                        .HasForeignKey("ParentJobId");

                    b.Navigation("ParentJob");
                });

            modelBuilder.Entity("API.Schema.Link", b =>
                {
                    b.HasOne("API.Schema.Manga", null)
                        .WithMany("Links")
                        .HasForeignKey("MangaId");
                });

            modelBuilder.Entity("API.Schema.Manga", b =>
                {
                    b.HasOne("API.Schema.MangaConnectors.MangaConnector", "MangaConnector")
                        .WithMany()
                        .HasForeignKey("MangaConnectorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MangaConnector");
                });

            modelBuilder.Entity("API.Schema.MangaAltTitle", b =>
                {
                    b.HasOne("API.Schema.Manga", null)
                        .WithMany("AltTitles")
                        .HasForeignKey("MangaId");
                });

            modelBuilder.Entity("AuthorManga", b =>
                {
                    b.HasOne("API.Schema.Author", null)
                        .WithMany()
                        .HasForeignKey("AuthorsAuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Schema.Manga", null)
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MangaMangaTag", b =>
                {
                    b.HasOne("API.Schema.Manga", null)
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Schema.MangaTag", null)
                        .WithMany()
                        .HasForeignKey("MangaTagsTag")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadAvailableChaptersJob", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadMangaCoverJob", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadSingleChapterJob", b =>
                {
                    b.HasOne("API.Schema.Chapter", "Chapter")
                        .WithMany()
                        .HasForeignKey("ChapterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chapter");
                });

            modelBuilder.Entity("API.Schema.Jobs.RetrieveChaptersJob", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Jobs.UpdateFilesDownloadedJob", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Jobs.UpdateMetadataJob", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Jobs.Job", b =>
                {
                    b.Navigation("DependsOnJobs");
                });

            modelBuilder.Entity("API.Schema.Manga", b =>
                {
                    b.Navigation("AltTitles");

                    b.Navigation("Links");
                });
#pragma warning restore 612, 618
        }
    }
}
