﻿// <auto-generated />
using System;
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
    [Migration("20241201223804_remove_job_returnvalue")]
    partial class remove_job_returnvalue
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Schema.Author", b =>
                {
                    b.Property<string>("AuthorId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("AuthorName")
                        .IsRequired()
                        .HasColumnType("text");

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
                        .HasColumnType("text");

                    b.Property<string>("ChapterIds")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float>("ChapterNumber")
                        .HasColumnType("real");

                    b.Property<bool>("Downloaded")
                        .HasColumnType("boolean");

                    b.Property<string>("ParentMangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<float?>("VolumeNumber")
                        .HasColumnType("real");

                    b.HasKey("ChapterId");

                    b.HasIndex("ParentMangaId");

                    b.ToTable("Chapters");
                });

            modelBuilder.Entity("API.Schema.Jobs.Job", b =>
                {
                    b.Property<string>("JobId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.PrimitiveCollection<string[]>("DependsOnJobIds")
                        .HasMaxLength(64)
                        .HasColumnType("text[]");

                    b.Property<string>("JobId1")
                        .HasColumnType("character varying(64)");

                    b.Property<byte>("JobType")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("LastExecution")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("NextExecution")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ParentJobId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<decimal>("RecurrenceMs")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("state")
                        .HasColumnType("integer");

                    b.HasKey("JobId");

                    b.HasIndex("JobId1");

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
                        .HasColumnType("text");

                    b.Property<string>("BaseUrl")
                        .IsRequired()
                        .HasColumnType("text");

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

                    b.Property<string>("LinkIds")
                        .HasColumnType("text");

                    b.Property<string>("LinkProvider")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LinkUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MangaId")
                        .IsRequired()
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

                    b.PrimitiveCollection<string[]>("AltTitleIds")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.PrimitiveCollection<string[]>("AuthorIds")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("ConnectorId")
                        .IsRequired()
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

                    b.Property<float>("IgnoreChapterBefore")
                        .HasColumnType("real");

                    b.Property<string>("LatestChapterAvailableId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("LatestChapterDownloadedId")
                        .HasColumnType("character varying(64)");

                    b.PrimitiveCollection<string[]>("LinkIds")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("MangaConnectorName")
                        .IsRequired()
                        .HasColumnType("character varying(32)");

                    b.Property<string>("MangaIds")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OriginalLanguage")
                        .HasColumnType("text");

                    b.Property<byte>("ReleaseStatus")
                        .HasColumnType("smallint");

                    b.PrimitiveCollection<string[]>("TagIds")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<long>("year")
                        .HasColumnType("bigint");

                    b.HasKey("MangaId");

                    b.HasIndex("LatestChapterAvailableId")
                        .IsUnique();

                    b.HasIndex("LatestChapterDownloadedId")
                        .IsUnique();

                    b.HasIndex("MangaConnectorName");

                    b.ToTable("Manga");
                });

            modelBuilder.Entity("API.Schema.MangaAltTitle", b =>
                {
                    b.Property<string>("AltTitleId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("AltTitleIds")
                        .HasColumnType("text");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("AltTitleId");

                    b.HasIndex("MangaId");

                    b.ToTable("AltTitles");
                });

            modelBuilder.Entity("API.Schema.MangaConnector", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.PrimitiveCollection<string[]>("BaseUris")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.PrimitiveCollection<string[]>("SupportedLanguages")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.HasKey("Name");

                    b.ToTable("MangaConnectors");
                });

            modelBuilder.Entity("API.Schema.MangaTag", b =>
                {
                    b.Property<string>("Tag")
                        .HasColumnType("text");

                    b.HasKey("Tag");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("API.Schema.NotificationConnectors.NotificationConnector", b =>
                {
                    b.Property<string>("NotificationConnectorId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<byte>("NotificationConnectorType")
                        .HasColumnType("smallint");

                    b.HasKey("NotificationConnectorId");

                    b.ToTable("NotificationConnectors");

                    b.HasDiscriminator<byte>("NotificationConnectorType");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("MangaAuthor", b =>
                {
                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("AuthorId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("AuthorIds")
                        .HasColumnType("text");

                    b.Property<string>("MangaIds")
                        .HasColumnType("text");

                    b.HasKey("MangaId", "AuthorId");

                    b.HasIndex("AuthorId");

                    b.ToTable("MangaAuthor");
                });

            modelBuilder.Entity("MangaTag", b =>
                {
                    b.Property<string>("MangaId")
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Tag")
                        .HasColumnType("text");

                    b.Property<string>("MangaIds")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TagIds")
                        .HasColumnType("text");

                    b.HasKey("MangaId", "Tag");

                    b.HasIndex("MangaIds");

                    b.HasIndex("Tag");

                    b.ToTable("MangaTag");
                });

            modelBuilder.Entity("API.Schema.Jobs.CreateArchiveJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("ChapterId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("ImagesLocation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasIndex("ChapterId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("ChapterId")
                                .HasColumnName("CreateArchiveJob_ChapterId");
                        });

                    b.HasDiscriminator().HasValue((byte)4);
                });

            modelBuilder.Entity("API.Schema.Jobs.CreateComicInfoXmlJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("ChapterId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasIndex("ChapterId");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("ChapterId")
                                .HasColumnName("CreateComicInfoXmlJob_ChapterId");
                        });

                    b.HasDiscriminator().HasValue((byte)6);
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadNewChaptersJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasIndex("MangaId");

                    b.HasDiscriminator().HasValue((byte)1);
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

            modelBuilder.Entity("API.Schema.Jobs.ProcessImagesJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<bool>("Bw")
                        .HasColumnType("boolean");

                    b.Property<int>("Compression")
                        .HasColumnType("integer");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("Jobs", t =>
                        {
                            t.Property("Path")
                                .HasColumnName("ProcessImagesJob_Path");
                        });

                    b.HasDiscriminator().HasValue((byte)5);
                });

            modelBuilder.Entity("API.Schema.Jobs.SearchMangaJob", b =>
                {
                    b.HasBaseType("API.Schema.Jobs.Job");

                    b.Property<string>("MangaConnectorName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SearchString")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue((byte)7);
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

            modelBuilder.Entity("API.Schema.NotificationConnectors.Gotify", b =>
                {
                    b.HasBaseType("API.Schema.NotificationConnectors.NotificationConnector");

                    b.Property<string>("AppToken")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue((byte)0);
                });

            modelBuilder.Entity("API.Schema.NotificationConnectors.Lunasea", b =>
                {
                    b.HasBaseType("API.Schema.NotificationConnectors.NotificationConnector");

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue((byte)1);
                });

            modelBuilder.Entity("API.Schema.NotificationConnectors.Ntfy", b =>
                {
                    b.HasBaseType("API.Schema.NotificationConnectors.NotificationConnector");

                    b.Property<string>("Auth")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Topic")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("NotificationConnectors", t =>
                        {
                            t.Property("Endpoint")
                                .HasColumnName("Ntfy_Endpoint");
                        });

                    b.HasDiscriminator().HasValue((byte)2);
                });

            modelBuilder.Entity("API.Schema.Chapter", b =>
                {
                    b.HasOne("API.Schema.Manga", "ParentManga")
                        .WithMany("Chapters")
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
                });

            modelBuilder.Entity("API.Schema.Link", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany("Links")
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("API.Schema.Manga", b =>
                {
                    b.HasOne("API.Schema.Chapter", "LatestChapterAvailable")
                        .WithOne()
                        .HasForeignKey("API.Schema.Manga", "LatestChapterAvailableId");

                    b.HasOne("API.Schema.Chapter", "LatestChapterDownloaded")
                        .WithOne()
                        .HasForeignKey("API.Schema.Manga", "LatestChapterDownloadedId");

                    b.HasOne("API.Schema.MangaConnector", "MangaConnector")
                        .WithMany("Mangas")
                        .HasForeignKey("MangaConnectorName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LatestChapterAvailable");

                    b.Navigation("LatestChapterDownloaded");

                    b.Navigation("MangaConnector");
                });

            modelBuilder.Entity("API.Schema.MangaAltTitle", b =>
                {
                    b.HasOne("API.Schema.Manga", "Manga")
                        .WithMany("AltTitles")
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manga");
                });

            modelBuilder.Entity("MangaAuthor", b =>
                {
                    b.HasOne("API.Schema.Author", null)
                        .WithMany()
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Schema.Manga", null)
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MangaTag", b =>
                {
                    b.HasOne("API.Schema.Manga", null)
                        .WithMany()
                        .HasForeignKey("MangaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Schema.MangaTag", null)
                        .WithMany()
                        .HasForeignKey("MangaIds")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Schema.MangaTag", null)
                        .WithMany()
                        .HasForeignKey("Tag")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("API.Schema.Jobs.CreateArchiveJob", b =>
                {
                    b.HasOne("API.Schema.Chapter", "Chapter")
                        .WithMany()
                        .HasForeignKey("ChapterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chapter");
                });

            modelBuilder.Entity("API.Schema.Jobs.CreateComicInfoXmlJob", b =>
                {
                    b.HasOne("API.Schema.Chapter", "Chapter")
                        .WithMany()
                        .HasForeignKey("ChapterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chapter");
                });

            modelBuilder.Entity("API.Schema.Jobs.DownloadNewChaptersJob", b =>
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

                    b.Navigation("Chapters");

                    b.Navigation("Links");
                });

            modelBuilder.Entity("API.Schema.MangaConnector", b =>
                {
                    b.Navigation("Mangas");
                });
#pragma warning restore 612, 618
        }
    }
}
