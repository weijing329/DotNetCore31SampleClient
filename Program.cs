using System;
using DotNetCore31SampleClient.Example;
using DotNetCore31SampleClient.Example.Quartz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DotNetCore31SampleClient
{
  class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
              services.AddLogging();
              services.AddSingleton<IGoogleCloudPubSubClient>(serviceProvider =>
                new GoogleCloudPubSubClient(serviceProvider.GetService<ILoggerFactory>().CreateLogger<GoogleCloudPubSubClient>()));
              services.AddSingleton<IReactiveRpcClient>(serviceProvider =>
                new ReactiveRpcClient(
                  serviceProvider.GetService<ILoggerFactory>().CreateLogger<ReactiveRpcClient>(),
                  serviceProvider.GetRequiredService<IGoogleCloudPubSubClient>()));

              // Add Quartz services
              services.AddQuartz(q =>
              {
                // handy when part of cluster or you want to otherwise identify multiple schedulers
                q.SchedulerId = "PullingJob-Scheduler";

                // we take this from appsettings.json, just show it's possible
                q.SchedulerName = "PullingJob Scheduler";

                // we could leave DI configuration intact and then jobs need
                // to have public no-arg constructor
                // the MS DI is expected to produce transient job instances
                // this WONT'T work with scoped services like EF Core's DbContext
                q.UseMicrosoftDependencyInjectionJobFactory(options =>
                {
                  // if we don't have the job in DI, allow fallback 
                  // to configure via default constructor
                  options.AllowDefaultConstructor = true;
                }); // ScopedJobFactory somehow whill block google pubsub pulling messages

                // or for scoped service support like EF Core DbContext
                // q.UseMicrosoftDependencyInjectionScopedJobFactory();

                // these are the defaults
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp =>
                {
                  tp.MaxConcurrency = 10;
                });

                // configure jobs with code
                var pullingJobKey = new JobKey("PullingJob", "DefaultJobGroup");
                q.AddJob<PullingJob>(j => j
                    .StoreDurably()
                    .WithIdentity(pullingJobKey)
                    .WithDescription("PullingJob")
                );

                q.AddTrigger(t => t
                    .WithIdentity("PullingJobTrigger")
                    .ForJob(pullingJobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromSeconds(5)).RepeatForever())
                    .WithDescription("PullingJobTrigger")
                );

                var publishingJobKey = new JobKey("PublishingJob", "DefaultJobGroup");
                q.AddJob<PublishingJob>(j => j
                    .StoreDurably()
                    .WithIdentity(publishingJobKey)
                    .WithDescription("PublishingJob")
                );

                q.AddTrigger(t => t
                    .WithIdentity("PublishingJobTrigger")
                    .ForJob(publishingJobKey)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMilliseconds(500)).RepeatForever())
                    .WithDescription("PublishingJobTrigger")
                );
              });

              // Quartz.Extensions.Hosting hosting
              services.AddQuartzHostedService(options =>
                        {
                          // when shutting down we want jobs to complete gracefully
                          options.WaitForJobsToComplete = true;
                        });

              services.AddHostedService<HostedService>();
            });
  }
}
