using Codenames;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    public class CodenamesDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Answer> Answers { get; set; }

        public CodenamesDbContext(DbContextOptions<CodenamesDbContext> options) : base(options)
        { 
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<User>()
                .HasMany(x => x.Answers)
                .WithOne(x => x.User)
                .HasForeignKey("UserId");

            builder.Entity<Game>()
                .HasMany(x => x.Words)
                .WithOne(x => x.Game)
                .HasForeignKey("GameId");
            builder.Entity<Game>()
                .HasMany(x => x.Answers)
                .WithOne(x => x.Game)
                .HasForeignKey("GameId");
            builder.Entity<Game>()
                .Ignore(x => x.State);

            builder.Entity<Vote>()
                .HasKey(new string[]{ "UserId", "GameId" });
            builder.Entity<Vote>()
                .HasOne(x => x.User)
                .WithMany(x => x.Votes)
                .HasForeignKey("UserId");
            builder.Entity<Vote>()
                .HasOne(x => x.Game)
                .WithMany(x => x.Votes)
                .HasForeignKey("GameId");

            builder.Entity<Answer>()
                .HasKey(new string[] { "UserId", "GameId" });
            builder.Entity<Answer>()
                .HasOne(x => x.User)
                .WithMany(x => x.Answers)
                .HasForeignKey("UserId");
            builder.Entity<Answer>()
                .HasOne(x => x.Game)
                .WithMany(x => x.Answers)
                .HasForeignKey("GameId");

            builder.Entity<Word>()
                .HasKey(new string[] { "GameId", "Value" });
        }
    }
}
