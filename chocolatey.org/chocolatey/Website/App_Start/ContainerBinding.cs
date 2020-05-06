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
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using AnglicanGeek.MarkdownMailer;
using Glav.CacheAdapter.Core.DependencyInjection;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NugetGallery;
using SimpleInjector;

namespace NuGetGallery
{
    /// <summary>
    ///   The main inversion container registration for the application. Look for other container bindings in client projects.
    /// </summary>
    public class ContainerBinding
    {
        private const int LUCENE_TIMEOUT_SECONDS = 720;

        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        public void RegisterComponents(Container container)
        {
            IConfiguration configuration = new Configuration();
            container.Register(() => configuration, Lifestyle.Singleton);

            //var gallerySetting = new Lazy<GallerySetting>(
            //    () =>
            //    {
            //        using (var entitiesContext = new EntitiesContext())
            //        {
            //            var settingsRepo = new EntityRepository<GallerySetting>(entitiesContext);
            //            return settingsRepo.GetAll().FirstOrDefault();
            //        }
            //    });

            //container.Register(() => gallerySetting.Value);
            //Bind<GallerySetting>().ToMethod(c => gallerySetting.Value);

            if (configuration.UseCaching)
            {
                var cacheProvider = AppServices.Cache;
                Cache.InitializeWith(cacheProvider);
                container.Register(() => cacheProvider, Lifestyle.Singleton);
            }

            container.RegisterPerWebRequest<ISearchService>(() => new LuceneSearchService(configuration.IndexContainsAllVersions));
            container.RegisterPerWebRequest<IEntitiesContext>(() => new EntitiesContext());
            container.RegisterPerWebRequest<IEntityRepository<User>>(() => new EntityRepository<User>(container.GetInstance<IEntitiesContext>()) {TraceLogEvents = true});
            container.RegisterPerWebRequest<IEntityRepository<PackageRegistration>, EntityRepository<PackageRegistration>>();
            container.RegisterPerWebRequest<IEntityRepository<Package>>(() => new EntityRepository<Package>(container.GetInstance<IEntitiesContext>()) { TraceLogEvents = true });
            container.RegisterPerWebRequest<IEntityRepository<PackageAuthor>, EntityRepository<PackageAuthor>>();
            container.RegisterPerWebRequest<IEntityRepository<PackageFramework>, EntityRepository<PackageFramework>>();
            container.RegisterPerWebRequest<IEntityRepository<PackageDependency>, EntityRepository<PackageDependency>>();
            container.RegisterPerWebRequest<IEntityRepository<PackageFile>, EntityRepository<PackageFile>>();
            container.RegisterPerWebRequest<IEntityRepository<PackageStatistics>, EntityRepository<PackageStatistics>>();
            container.RegisterPerWebRequest<IEntityRepository<PackageOwnerRequest>, EntityRepository<PackageOwnerRequest>>();

            container.RegisterPerWebRequest<IUserService, UserService>();
            container.RegisterPerWebRequest<IPackageService, PackageService>();
            container.RegisterPerWebRequest<ICryptographyService, CryptographyService>();

            container.Register<IIndexingService>(() => new LuceneIndexingService(
                    () => new EntitiesContext(
                            configuration.UseBackgroundJobsDatabaseUser ? 
                              EntitiesContext.AdjustConnectionString("NuGetGallery", configuration.BackgroundJobsDatabaseUserId, configuration.BackgroundJobsDatabaseUserPassword)  
                            : "NuGetGallery",
                     LUCENE_TIMEOUT_SECONDS
                    ),
                    configuration.IndexContainsAllVersions), 
                    Lifestyle.Singleton);
            container.Register<IFormsAuthenticationService, FormsAuthenticationService>(Lifestyle.Singleton);

            container.RegisterPerWebRequest<IControllerFactory, NuGetControllerFactory>();
            container.RegisterPerWebRequest<INuGetExeDownloaderService, NuGetExeDownloaderService>();

            var mailSenderThunk = new Lazy<IMailSender>(
                () =>
                {
                    var settings = container.GetInstance<IConfiguration>();
                    if (settings.UseSmtp)
                    {
                        var mailSenderConfiguration = new MailSenderConfiguration
                        {
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            Host = settings.SmtpHost,
                            Port = settings.SmtpPort,
                            EnableSsl = configuration.SmtpEnableSsl,
                        };

                        if (!String.IsNullOrWhiteSpace(settings.SmtpUsername))
                        {
                            mailSenderConfiguration.UseDefaultCredentials = false;
                            mailSenderConfiguration.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
                        }

                        return new MailSender(mailSenderConfiguration);
                    }
                    else
                    {
                        var mailSenderConfiguration = new MailSenderConfiguration
                        {
                            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                            PickupDirectoryLocation = HostingEnvironment.MapPath("~/App_Data/Mail")
                        };

                        return new MailSender(mailSenderConfiguration);
                    }
                });

            container.Register(() => mailSenderThunk.Value, Lifestyle.Singleton);

            container.Register<IMessageService, MessageService>(Lifestyle.Singleton);
            container.Register<IFileSystemService, FileSystemService>(Lifestyle.Singleton);

            container.RegisterPerWebRequest(() => HttpContext.Current.User);
            //Bind<IPrincipal>().ToMethod(context => HttpContext.Current.User);

            switch (configuration.PackageStoreType)
            {
                case PackageStoreType.FileSystem:
                case PackageStoreType.NotSpecified:
                    container.Register<IFileStorageService, FileSystemFileStorageService>(Lifestyle.Singleton);
                    break;
                case PackageStoreType.AzureStorageBlob:
                    container.Register<ICloudBlobClient>(
                        () =>
                        new CloudBlobClientWrapper(
                            new CloudBlobClient(
                                new Uri(configuration.AzureStorageBlobUrl, UriKind.Absolute), new StorageCredentialsAccountAndKey(configuration.AzureStorageAccountName, configuration.AzureStorageAccessKey))),
                        Lifestyle.Singleton);
                    container.Register<IFileStorageService, CloudBlobFileStorageService>(Lifestyle.Singleton);
                    break;
                case PackageStoreType.AmazonS3Storage:
                    container.Register<IAmazonS3Client, AmazonS3ClientWrapper>(Lifestyle.Singleton);
                    container.Register<IFileStorageService, AmazonS3FileStorageService>(Lifestyle.Singleton);
                    break;
            }

            switch (configuration.PackageStatisticsStoreType)
            {
                case PackageStatisticsStoreType.AmazonSqs:
                    container.Register<IAmazonSqsClient, AmazonSqsClientWrapper>(Lifestyle.Singleton);
                    container.Register<IPackageStatisticsService, AmazonSqsPackageStatisticsService>(Lifestyle.Singleton);
                    break;
                default:
                    container.RegisterPerWebRequest<IPackageStatisticsService, DatabasePackageStatisticsService>();
                    break;
            }

            container.Register<IPackageFileService, PackageFileService>(Lifestyle.Singleton);
            container.Register<IUploadFileService, UploadFileService>();

            // todo: bind all package curators by convention
            container.Register<IAutomaticPackageCurator, WebMatrixPackageCurator>(Lifestyle.Singleton);
            container.Register<IAutomaticPackageCurator, Windows8PackageCurator>(Lifestyle.Singleton);

            // todo: bind all commands by convention
            container.RegisterPerWebRequest<IAutomaticallyCuratePackageCommand, AutomaticallyCuratePackageCommand>();
            container.RegisterPerWebRequest<ICreateCuratedPackageCommand, CreateCuratedPackageCommand>();
            container.RegisterPerWebRequest<IDeleteCuratedPackageCommand, DeleteCuratedPackageCommand>();
            container.RegisterPerWebRequest<IModifyCuratedPackageCommand, ModifyCuratedPackageCommand>();

            // todo: bind all queries by convention
            container.RegisterPerWebRequest<ICuratedFeedByKeyQuery, CuratedFeedByKeyQuery>();
            container.RegisterPerWebRequest<ICuratedFeedByNameQuery, CuratedFeedByNameQuery>();
            container.RegisterPerWebRequest<ICuratedFeedsByManagerQuery, CuratedFeedsByManagerQuery>();
            container.RegisterPerWebRequest<IPackageRegistrationByKeyQuery, PackageRegistrationByKeyQuery>();
            container.RegisterPerWebRequest<IPackageRegistrationByIdQuery, PackageRegistrationByIdQuery>();
            container.RegisterPerWebRequest<IUserByUsernameQuery, UserByUsernameQuery>();
            container.RegisterPerWebRequest<IPackageIdsQuery, PackageIdsQuery>();
            container.RegisterPerWebRequest<IPackageVersionsQuery, PackageVersionsQuery>();

            container.RegisterPerWebRequest<IAggregateStatsService, AggregateStatsService>();

            RegisterChocolateySpecific(container);
        }

        private void RegisterChocolateySpecific(Container container)
        {
            container.RegisterPerWebRequest<IUserSiteProfilesService, UserSiteProfilesService>();
            container.RegisterPerWebRequest<IEntityRepository<UserSiteProfile>, EntityRepository<UserSiteProfile>>();
            container.RegisterPerWebRequest<IEntityRepository<Course>, EntityRepository<Course>>();
            container.RegisterPerWebRequest<IEntityRepository<CourseModule>, EntityRepository<CourseModule>>();
            container.RegisterPerWebRequest<ICourseAchievementsService, CourseAchievementsService>();
            container.RegisterPerWebRequest<IEntityRepository<UserCourseAchievement>, EntityRepository<UserCourseAchievement>>();
            container.Register<IImageFileService, ImageFileService>(Lifestyle.Singleton);
            container.RegisterPerWebRequest<IEntityRepository<ScanResult>, EntityRepository<ScanResult>>();
            container.RegisterPerWebRequest<IScanService, ScanService>();
        }
    }
}
