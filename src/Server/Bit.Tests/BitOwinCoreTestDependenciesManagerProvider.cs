﻿using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Implementations;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Bit.Test;
using Bit.Tests.Api.Implementations.Project;
using Bit.Tests.Api.Middlewares;
using Bit.Tests.Data.Implementations;
using Bit.Tests.IdentityServer.Implementations;
using Bit.Tests.Model.Implementations;
using Bit.Tests.Properties;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using Bit.Hangfire.Implementations;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.Signalr.Implementations;
using System.Data.SqlClient;

namespace Bit.Tests
{
    public class BitOwinCoreTestDependenciesManagerProvider : IAspNetCoreDependenciesManager, IDependenciesManagerProvider
    {
        private readonly TestEnvironmentArgs _args;

        protected BitOwinCoreTestDependenciesManagerProvider()
        {
        }


        public BitOwinCoreTestDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<IRequestInformationProvider, AspNetCoreRequestInformationProvider>();
            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.RegisterLogStore<ConsoleLogStore>();
            dependencyManager.RegisterLogStore<DebugLogStore>();
            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();
            dependencyManager.RegisterAppEvents<InitialTestDataConfiguration>();

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreStaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreSingleSignOnClient();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogUserInformationMiddlewareConfiguration>();

            dependencyManager.RegisterAspNetCoreMiddleware<TestWebApiCoreMvcMiddlewareConfiguration>();

            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiODataConfiguration();

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new AuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration("WebApi");

                }).Resolve<IOwinMiddlewareConfiguration>("WebApi");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<BitEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<TestEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IODataSqlBuilder, DefaultODataSqlBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration();

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata();

            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(TestEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterGeneric(typeof(IEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), typeof(TestEfEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            if (Settings.Default.UseInMemoryProviderByDefault)
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, InMemoryDbContextObjectsProvider>();
            else
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, SqlDbContextObjectsProvider>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<TestDtoModelMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<TestUserService, TestClientProvider>();

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();
        }

        public virtual void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager)
        {
            services.AddWebApiCore();
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }
    }
}
