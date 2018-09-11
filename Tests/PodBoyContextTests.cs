using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using PodBoy.Context;
using PodBoy.Entity;
using ReactiveUI;
using Xunit;

namespace Tests
{
    public class PodBoyContextTests
    {
        public PodBoyContextTests()
        {
            var dir = new DirectoryInfo(".").Parent?.Parent?.Parent;
            AppDomain.CurrentDomain.SetData("DataDirectory", dir?.FullName);

            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
        }

        [Fact]
        public async Task AddChannelTest()
        {
            using (var db = new PodBoyContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    var channel = db.Channels.Add(new Channel("test")
                    {
                        Title = "Test Channel",
                        Description = "This is a test!"
                    });
                    channel.Episodes.Add(new Episode
                    {
                        Media = new Media()
                    });

                    await db.SaveChangesAsync();

                    Assert.NotEqual(0, channel.Id);

                    transaction.Rollback();
                }
            }
        }
    }
}