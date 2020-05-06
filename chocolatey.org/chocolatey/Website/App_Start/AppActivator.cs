// Copyright 2011 - Present RealDimensions Software, LLC, the original
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery
// at https://github.com/NuGet/NuGetGallery
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Elmah;
using Elmah.Contrib.Mvc;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using NuGetGallery;
using NuGetGallery.Jobs;
using NuGetGallery.Migrations;
using NuGetGallery.MvcOverrides;
using StackExchange.Profiling;
using StackExchange.Profiling.MVCHelpers;
using WebActivator;
using WebBackgrounder;

[assembly: WebActivator.PreApplicationStartMethod(typeof(AppActivator), "PreStart")]
[assembly: PostApplicationStartMethod(typeof(AppActivator), "PostStart")]
[assembly: ApplicationShutdownMethod(typeof(AppActivator), "Stop")]

namespace NuGetGallery
{
    public static class AppActivator
    {
        private static JobManager _jobManager;

        public static void PreStart()
        {
            MiniProfilerPreStart();
        }

        public static void PostStart()
        {
            MiniProfilerPostStart();
            //todo: this is how database is automatically updated
            //DbMigratorPostStart();
            BackgroundJobsPostStart();
            AppPostStart();
        }

        public static void Stop()
        {
            BackgroundJobsStop();
        }

        private static void AppPostStart()
        {
            ModelBinders.Binders.Add(typeof(PackageScanResult), new BaseModelBinder<PackageScanResult>());

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new CSharpRazorViewEngine());

            RegisterGlobalFilters(GlobalFilters.Filters);

            Routes.RegisterRoutes(RouteTable.Routes);

#if !DEBUG
            Database.SetInitializer<EntitiesContext>(null);
#else
            var ensureImportsForRelease = Database.DefaultConnectionFactory;
#endif

            ValueProviderFactories.Factories.Add(new HttpHeaderValueProviderFactory());
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ElmahHandleErrorAttribute());

            if (ConfigurationManager.AppSettings.Get("ForceSSL")
                                    .Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase)) filters.Add(new RequireHttpsAppHarborAttribute());
        }

        private static void BackgroundJobsPostStart()
        {
            var jobs = new List<IJob>();
            var indexer = DependencyResolver.Current.GetService<IIndexingService>();
            var configuration = DependencyResolver.Current.GetService<IConfiguration>();

            if (indexer != null)
            {
                indexer.RegisterBackgroundJobs(jobs);
            }

            var connectionString = configuration.UseBackgroundJobsDatabaseUser
                ? EntitiesContext.AdjustConnectionString("NuGetGallery", configuration.BackgroundJobsDatabaseUserId, configuration.BackgroundJobsDatabaseUserPassword)
                : "NuGetGallery";

            if (ConfigurationManager.AppSettings.Get("EnablePackageStatisticsBackgroundJob").Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
            {
                jobs.Add(new UpdateStatisticsJob(TimeSpan.FromMinutes(15), () => new EntitiesContext(connectionString), timeout: TimeSpan.FromMinutes(30)));
            }

            if (ConfigurationManager.AppSettings.Get("EnableWorkItemsBackgroundJob").Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
            {
                jobs.Add(new WorkItemCleanupJob(TimeSpan.FromDays(1), () => new EntitiesContext(connectionString), timeout: TimeSpan.FromDays(4)));
            }

            var jobCoordinator = new WebFarmJobCoordinator(new EntityWorkItemRepository(() => new EntitiesContext(connectionString)));
            _jobManager = new JobManager(jobs, jobCoordinator)
            {
                RestartSchedulerOnFailure = true
            };
            _jobManager.Fail(e => ErrorLog.GetDefault(null).Log(new Error(e)));
            _jobManager.Start();
        }

        private static void BackgroundJobsStop()
        {
            _jobManager.Dispose();
        }

        private static void DbMigratorPostStart()
        {
            var dbMigrator = new DbMigrator(new MigrationsConfiguration());
            // After upgrading to EF 4.3 and MiniProfile 1.9, there is a bug that causes several
            // 'Invalid object name 'dbo.__MigrationHistory' to be thrown when the database is first created;
            // it seems these can safely be ignored, and the database will still be created.
            dbMigrator.Update();
        }

        private static void MiniProfilerPreStart()
        {
#if DEBUG
            MiniProfilerEF.Initialize();
            DynamicModuleUtility.RegisterModule(typeof(MiniProfilerStartupModule));
            GlobalFilters.Filters.Add(new ProfilingActionFilter());
#endif
        }

        private static void MiniProfilerPostStart()
        {
#if DEBUG
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
#endif
        }

        private class MiniProfilerStartupModule : IHttpModule
        {
            public void Init(HttpApplication context)
            {
                context.BeginRequest += (sender, e) => MiniProfiler.Start();

                context.AuthorizeRequest += (sender, e) =>
                {
                    bool stopProfiling;
                    var httpContext = HttpContext.Current;

                    if (httpContext == null) stopProfiling = true;
                    else
                    {
                        // Temporarily removing until we figure out the hammering of request we saw.
                        //var userCanProfile = httpContext.User != null && HttpContext.Current.User.IsInRole(Const.AdminRoleName);
                        var requestIsLocal = httpContext.Request.IsLocal;

                        //stopProfiling = !userCanProfile && !requestIsLocal
                        stopProfiling = !requestIsLocal;
                    }

                    if (stopProfiling) MiniProfiler.Stop(true);
                };

                context.EndRequest += (sender, e) => MiniProfiler.Stop();
            }

            public void Dispose()
            {
            }
        }
    }
}
