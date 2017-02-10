using Microsoft.EntityFrameworkCore;
using SharedMoleRes.Client;
using SharedMoleRes.Server.Surrogates;

namespace MoleConsApp1
{
    public class UserFormContext : DbContext
    {
        public DbSet<UserFormSurrogate> Users { get; set; }
        public DbSet<AuthenticationFormSur> AuthForms { get; set; }
        public DbSet<AccessibilityInfoSur> AccesForms { get; set; }
        public DbSet<PublicKeyForm> PublicKeys { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=UserFormDb;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserFormSurrogate>().HasKey(surrogate => surrogate.Id);
            modelBuilder.Entity<UserFormSurrogate>().HasIndex(surrogate => surrogate.Login).IsUnique(true);
            modelBuilder.Entity<UserFormSurrogate>()
                .HasOne(sur => sur.Accessibility)
                .WithOne(sur => sur.UserForm)
                .HasForeignKey<AccessibilityInfoSur>(sur => sur.UserFormId);
            modelBuilder.Entity<UserFormSurrogate>()
                .HasOne(sur => sur.AuthenticationForm)
                .WithOne(sur => sur.UserForm)
                .HasForeignKey<AuthenticationFormSur>(sur => sur.UserFormId);
            modelBuilder.Entity<UserFormSurrogate>()
                .HasOne(sur => sur.KeyParametrsBlob)
                .WithOne(form => form.UserForm)
                .HasForeignKey<PublicKeyForm>(form => form.UserFormId);

            //modelBuilder.Entity<AuthenticationFormSur>()
        }
    }
}
