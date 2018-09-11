using System.Data.Entity;
using PodBoy.Entity;
using PodBoy.Playlists;

namespace PodBoy.Context
{
    public class PodBoyContext : DbContext
    {
        public virtual DbSet<Channel> Channels { get; set; }
        public virtual DbSet<Episode> Episodes { get; set; }

        public virtual DbSet<Playlist> Playlists { get; set; }
        public virtual DbSet<PlaylistItem> PlaylistItems { get; set; }

        public PodBoyContext()
        {
            Database.SetInitializer<PodBoyContext>(null);
            //Configuration.LazyLoadingEnabled = false;

            //Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // channel
            modelBuilder.Entity<Channel>().ToTable("channel");
            modelBuilder.Entity<Channel>().HasKey(_ => _.Id).Property(x => x.Id).HasColumnName("id").HasColumnOrder(0);

            modelBuilder.Entity<Channel>().Ignore(_ => _.UnplayedCount);
            modelBuilder.Entity<Channel>().Ignore(_ => _.ImageProvider);

            modelBuilder.Entity<Channel>().Property(_ => _.LastUpdated).HasColumnName("lastUpdated");
            modelBuilder.Entity<Channel>().Property(_ => _.Description).HasColumnName("description");
            modelBuilder.Entity<Channel>().Property(_ => _.Link).HasColumnName("atomLink");
            modelBuilder.Entity<Channel>().Property(_ => _.ImageUrl).HasColumnName("imageUrl");
            modelBuilder.Entity<Channel>().Property(_ => _.OrderNumber).HasColumnName("orderNumber");

            // episode
            modelBuilder.Entity<Episode>().ToTable("episode");
            modelBuilder.Entity<Episode>().HasKey(_ => _.Id).Property(x => x.Id).HasColumnName("id").HasColumnOrder(0);

            modelBuilder.Entity<Episode>().Ignore(_ => _.IsSelected);
            modelBuilder.Entity<Episode>().Ignore(_ => _.IsActive);
            modelBuilder.Entity<Episode>().Ignore(_ => _.IsPlaying);
            modelBuilder.Entity<Episode>().Ignore(_ => _.ImageProvider);

            modelBuilder.Entity<Episode>().Property(_ => _.Guid).HasColumnName("guid");
            modelBuilder.Entity<Episode>().Property(_ => _.Description).HasColumnName("description");
            modelBuilder.Entity<Episode>().Property(_ => _.Link).HasColumnName("link");
            modelBuilder.Entity<Episode>().Property(_ => _.Title).HasColumnName("title");
            modelBuilder.Entity<Episode>().Property(_ => _.Date).HasColumnName("date");
            modelBuilder.Entity<Episode>().Property(_ => _.ImageUrl).HasColumnName("imageUrl");
            modelBuilder.Entity<Episode>().Property(_ => _.IsPlayed).HasColumnName("played");
            modelBuilder.Entity<Episode>().Property(_ => _.ChannelId).HasColumnName("channelId");

            modelBuilder.Entity<Episode>()
                .HasRequired(e => e.Channel)
                .WithMany(c => c.Episodes)
                .HasForeignKey(_ => _.ChannelId);

            // media
            modelBuilder.ComplexType<Media>().Property(_ => _.Url).HasColumnName("mediaUrl");
            modelBuilder.ComplexType<Media>().Property(_ => _.Length).HasColumnName("mediaLength");
            modelBuilder.ComplexType<Media>().Property(_ => _.MediaType).HasColumnName("mediaType");

            // playlist
            modelBuilder.Entity<Playlist>().ToTable("playlist");
            modelBuilder.Entity<Playlist>().HasKey(_ => _.Id).Property(x => x.Id).HasColumnName("id").HasColumnOrder(0);

            modelBuilder.Entity<Playlist>().Ignore(_ => _.ItemsCount);
            modelBuilder.Entity<Playlist>().Ignore(_ => _.IsSelected);

            modelBuilder.Entity<Playlist>().Ignore(_ => _.Previous);
            modelBuilder.Entity<Playlist>().Ignore(_ => _.Next);

            modelBuilder.Entity<Playlist>().Ignore(_ => _.Type);

            modelBuilder.Entity<Playlist>().Property(_ => _.Name).HasColumnName("name");
            modelBuilder.Entity<Playlist>().Property(_ => _.OrderNumber).HasColumnName("orderNumber");

            modelBuilder.Entity<Playlist>()
                .HasOptional(_ => _.Current)
                .WithMany()
                .HasForeignKey(_ => _.CurrentId)
                .WillCascadeOnDelete();

            // playlist item
            modelBuilder.Entity<PlaylistItem>().ToTable("playlistItem");
            modelBuilder.Entity<PlaylistItem>()
                .HasKey(_ => _.Id)
                .Property(_ => _.Id)
                .HasColumnName("id")
                .HasColumnOrder(0);

            modelBuilder.Entity<PlaylistItem>().Ignore(_ => _.IsActive);
            modelBuilder.Entity<PlaylistItem>().Ignore(_ => _.IsPlaying);
            modelBuilder.Entity<PlaylistItem>().Ignore(_ => _.IsPlayed);

            modelBuilder.Entity<PlaylistItem>().Property(_ => _.OrderNumber).HasColumnName("orderNumber");
            modelBuilder.Entity<PlaylistItem>()
                .HasRequired(_ => _.Item)
                .WithMany()
                .HasForeignKey(_ => _.ItemId)
                .WillCascadeOnDelete();

            modelBuilder.Entity<PlaylistItem>()
                .HasRequired(_ => _.Playlist)
                .WithMany(_ => _.Items)
                .HasForeignKey(_ => _.PlaylistId)
                .WillCascadeOnDelete();
        }

        public virtual void Detach<T>(T entity) where T : class, IEntity
        {
            Entry(entity).State = EntityState.Detached;
        }

        public virtual void SetModified<T>(T entity) where T : class, IEntity
        {
            Entry(entity).State = EntityState.Modified;
        }

        public virtual bool IsDetached<T>(T entity) where T : class, IEntity
        {
            return Entry(entity).State == EntityState.Detached;
        }
    }
}