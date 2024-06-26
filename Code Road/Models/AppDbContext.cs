﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Code_Road.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Image> Image { get; set; }
        public DbSet<Follow> Follow { get; set; }
        public DbSet<FinishedLessons> FinishedLessons { get; set; }
        public DbSet<CommentVote> Comments_Vote { get; set; }
        public DbSet<PostVote> Posts_Vote { get; set; }

        public AppDbContext()
        {

        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Image>(entity =>
            {
                entity.HasIndex(e => e.UserId).IsUnique(false);
            });
            builder.Entity<Follow>(enitity =>
            {
                enitity.HasKey(k => new { k.FollowerId, k.FollowingId });
            });
            builder.Entity<FinishedLessons>(enitity =>
            {
                enitity.HasKey(k => new { k.UserId, k.LessonId });
            });
        }
    }
}
