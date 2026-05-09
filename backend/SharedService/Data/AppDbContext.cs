using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }

        public DbSet<CallSession> CallSessions => Set<CallSession>();
        public DbSet<CallParticipant> CallParticipants => Set<CallParticipant>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // MessageReadReceipt composite PK
            builder.Entity<MessageReadReceipt>()
                .HasKey(MessageReadReceipt => new { MessageReadReceipt.MessageId, MessageReadReceipt.UserId });

            builder.Entity<MessageReadReceipt>()
                .HasOne(r => r.Message)         
                .WithMany(m => m.ReadReceipts) 
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. User Relationship (NoAction to avoid circular paths)
            builder.Entity<MessageReadReceipt>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Conversation>()
                .HasMany(c => c.Participants)
                .WithOne(p => p.Conversation)
                .HasForeignKey(p => p.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Conversation>()
             .HasMany(c => c.Messages)
             .WithOne(m => m.Conversation)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            // Indices for performance
            builder.Entity<Message>()
                .HasIndex(m => new { m.ConversationId, m.SentAt });

            builder.Entity<ConversationParticipant>()
                .HasIndex(p => p.UserId);

            builder.Entity<FriendRequest>()
                .HasIndex(fr => new { fr.FromUserId, fr.ToUserId, fr.Status });

            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.FromUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure the Receiver relationship
            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ToUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // calls 
            builder.Entity<CallParticipant>()
                .HasOne(p => p.CallSession)
                .WithMany(c => c.Participants)
                .HasForeignKey(p => p.CallId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CallSession>()
                .HasIndex(c => c.ConversationId);

            builder.Entity<CallSession>()
                .HasIndex(c => c.RoomName)
                .IsUnique();

        }
    }
}
