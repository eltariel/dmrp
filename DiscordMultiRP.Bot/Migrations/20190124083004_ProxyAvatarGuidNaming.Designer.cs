﻿// <auto-generated />
using System;
using DiscordMultiRP.Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DiscordMultiRP.Bot.Migrations
{
    [DbContext(typeof(ProxyDataContext))]
    [Migration("20190124083004_ProxyAvatarGuidNaming")]
    partial class ProxyAvatarGuidNaming
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.Channel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("DiscordId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<bool>("IsMonitored");

                    b.HasKey("Id");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.Proxy", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AvatarContentType");

                    b.Property<Guid>("AvatarGuid");

                    b.Property<bool>("IsGlobal");

                    b.Property<bool>("IsReset");

                    b.Property<string>("Name");

                    b.Property<string>("Prefix");

                    b.Property<string>("Suffix");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Proxies");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.ProxyChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ChannelId");

                    b.Property<int>("ProxyId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("ProxyId");

                    b.ToTable("ProxyChannels");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("DiscordId")
                        .HasConversion(new ValueConverter<decimal, decimal>(v => default(decimal), v => default(decimal), new ConverterMappingHints(precision: 20, scale: 0)));

                    b.Property<int>("Role");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.UserChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ChannelId");

                    b.Property<int?>("LastProxyId");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("LastProxyId");

                    b.HasIndex("UserId");

                    b.ToTable("UserChannels");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.Proxy", b =>
                {
                    b.HasOne("DiscordMultiRP.Bot.Data.User", "User")
                        .WithMany("Proxies")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.ProxyChannel", b =>
                {
                    b.HasOne("DiscordMultiRP.Bot.Data.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId");

                    b.HasOne("DiscordMultiRP.Bot.Data.Proxy", "Proxy")
                        .WithMany("Channels")
                        .HasForeignKey("ProxyId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DiscordMultiRP.Bot.Data.UserChannel", b =>
                {
                    b.HasOne("DiscordMultiRP.Bot.Data.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId");

                    b.HasOne("DiscordMultiRP.Bot.Data.Proxy", "LastProxy")
                        .WithMany()
                        .HasForeignKey("LastProxyId");

                    b.HasOne("DiscordMultiRP.Bot.Data.User", "User")
                        .WithMany("Channels")
                        .HasForeignKey("UserId");
                });
#pragma warning restore 612, 618
        }
    }
}
