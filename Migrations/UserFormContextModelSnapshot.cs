using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MoleConsApp1;
using SharedMoleRes.Server;

namespace MoleConsApp1.Migrations
{
    [DbContext(typeof(UserFormContext))]
    partial class UserFormContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SharedMoleRes.Client.PublicKeyForm", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CryptoAlg");

                    b.Property<string>("CryptoProvider");

                    b.Property<byte[]>("Hash");

                    b.Property<string>("HashAlg");

                    b.Property<byte[]>("Key");

                    b.Property<byte[]>("Sign");

                    b.Property<int>("UserFormId");

                    b.HasKey("Id");

                    b.HasIndex("UserFormId")
                        .IsUnique();

                    b.ToTable("PublicKeys");
                });

            modelBuilder.Entity("SharedMoleRes.Server.Surrogates.AccessibilityInfoSur", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConstUsers");

                    b.Property<bool>("IsPublicProfile");

                    b.Property<int>("MaxConstUsers");

                    b.Property<int>("MaxTempUsers");

                    b.Property<string>("TempUsers");

                    b.Property<int>("UserFormId");

                    b.HasKey("Id");

                    b.HasIndex("UserFormId")
                        .IsUnique();

                    b.ToTable("AccesForms");
                });

            modelBuilder.Entity("SharedMoleRes.Server.Surrogates.AuthenticationFormSur", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte>("AuthenticationMethod");

                    b.Property<string>("CryptoProvider");

                    b.Property<string>("Login");

                    b.Property<int>("UserFormId");

                    b.HasKey("Id");

                    b.HasIndex("UserFormId")
                        .IsUnique();

                    b.ToTable("AuthForms");
                });

            modelBuilder.Entity("SharedMoleRes.Server.Surrogates.UserFormSurrogate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Ip");

                    b.Property<string>("Login");

                    b.Property<string>("Password");

                    b.Property<int>("PortClientToClient1");

                    b.Property<int>("PortClientToClient2");

                    b.Property<int>("PortClientToClient3");

                    b.Property<int>("PortServerToClient");

                    b.HasKey("Id");

                    b.HasIndex("Login")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SharedMoleRes.Client.PublicKeyForm", b =>
                {
                    b.HasOne("SharedMoleRes.Server.Surrogates.UserFormSurrogate", "UserForm")
                        .WithOne("KeyParametrsBlob")
                        .HasForeignKey("SharedMoleRes.Client.PublicKeyForm", "UserFormId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SharedMoleRes.Server.Surrogates.AccessibilityInfoSur", b =>
                {
                    b.HasOne("SharedMoleRes.Server.Surrogates.UserFormSurrogate", "UserForm")
                        .WithOne("Accessibility")
                        .HasForeignKey("SharedMoleRes.Server.Surrogates.AccessibilityInfoSur", "UserFormId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SharedMoleRes.Server.Surrogates.AuthenticationFormSur", b =>
                {
                    b.HasOne("SharedMoleRes.Server.Surrogates.UserFormSurrogate", "UserForm")
                        .WithOne("AuthenticationForm")
                        .HasForeignKey("SharedMoleRes.Server.Surrogates.AuthenticationFormSur", "UserFormId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
