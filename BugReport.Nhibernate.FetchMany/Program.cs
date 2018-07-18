using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugReport.Nhibernate.FetchMany
{
    /// <summary>
    /// This test application executes a simple query fetching a user and the user's addresses through FetchMany. However when NhLogger performs certain calls
    /// to Microsoft.Extension.Logging.ILogger, the FetchMany query fails to execute with the error "Invalid attempt to read when no data is present."
    ///
    /// To switch the error on and off, comment and uncomment the instruction 'NHibernateLogger.SetLoggersFactory(new NHLoggerFactory(_loggerFactory));'
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var testQueryExecutor = serviceProvider.GetService<TestQueryExecutor>();
            testQueryExecutor.PerformTest();

            Console.ReadLine();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<NHibernateSessionProvider>()
                    .AddTransient<TestQueryExecutor>()
                    .AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace)); // Changing MinimumLevel to info or above also keeps the error from occuring
        }
    }

    public class TestQueryExecutor
    {
        private readonly NHibernateSessionProvider _nHibernateSessionProvider;

        public TestQueryExecutor(NHibernateSessionProvider nHibernateSessionProvider)
        {
            _nHibernateSessionProvider = nHibernateSessionProvider;
        }

        public void PerformTest()
        {
            using (var session = _nHibernateSessionProvider.GetNewSession())
            {
                var query = session.Query<Entities.User>().Where(u => u.IsDeleted == false).FetchMany(u => u.Addresses).ToList();
            }
        }
    }

    /// <summary>
    /// I'm aware that this implementation does not follow best practices. It was designed to reproduce the error
    /// </summary>
    public class NHibernateSessionProvider
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

        public NHibernateSessionProvider(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public ISession GetNewSession()
        {
            return GetNhSessionFactory().OpenSession();
        }

        private ISessionFactory GetNhSessionFactory()
        {
            NHibernateLogger.SetLoggersFactory(new NHLoggerFactory(_loggerFactory));

            var sessionFactory =
                Fluently.Configure()
                    .Database(
                        MsSqlConfiguration.MsSql2012.ConnectionString("Server=sqlserver;Database=TestDb;User Id=sa;Password=SApassword123__;MultipleActiveResultSets=True;Application Name=TestQueryExecutor")
                                          .UseOuterJoin()
                                          .IsolationLevel(System.Data.IsolationLevel.ReadCommitted))
                    .Mappings(m => m.FluentMappings.Add<Mappings.UserMapping>().Add<Mappings.UserAddressMapping>())
                    .BuildSessionFactory();

            return sessionFactory;
        }
    }

    public class Entities
    {
        public class User
        {
            public virtual long Id { get; set; }
            public virtual bool IsDeleted { get; set; }
            public virtual ICollection<UserAddress> Addresses { get; set; }
        }

        public class UserAddress
        {
            public virtual long Id { get; set; }
            public virtual bool IsDeleted { get; set; }
        }
    }

    public class Mappings
    {
        public class UserMapping : ClassMap<Entities.User>
        {
            public UserMapping()
            {
                Schema("[User]");
                Table("[User]");

                Id(m => m.Id).GeneratedBy.Identity();
                Map(m => m.IsDeleted);
                HasMany(m => m.Addresses).KeyColumn("UserId");
            }
        }

        public class UserAddressMapping : ClassMap<Entities.UserAddress>
        {
            public UserAddressMapping()
            {
                Schema("[User]");
                Table("[Address]");

                Id(m => m.Id).GeneratedBy.Identity();
                Map(m => m.IsDeleted);
            }
        }
    }

    public class NHLoggerFactory : INHibernateLoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

        public NHLoggerFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public INHibernateLogger LoggerFor(string keyName)
        {
            return new NHLogger(_loggerFactory.CreateLogger("Data.Nhibernate." + keyName));
        }

        public INHibernateLogger LoggerFor(Type type)
        {
            return new NHLogger(_loggerFactory.CreateLogger("Data.Nhibernate." + type.Name));
        }
    }

    public class NHLogger : INHibernateLogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public NHLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public bool IsEnabled(NHibernateLogLevel logLevel)
        {
            return _logger.IsEnabled(MapToCoreLogLevel(logLevel));
        }

        public void Log(NHibernateLogLevel logLevel, NHibernateLogValues state, Exception exception)
        {
            _logger.Log(MapToCoreLogLevel(logLevel), exception, state.Format, state.Args);
        }

        private Microsoft.Extensions.Logging.LogLevel MapToCoreLogLevel(NHibernateLogLevel nhLogLevel)
        {
            switch (nhLogLevel)
            {
                case NHibernateLogLevel.Trace:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;

                case NHibernateLogLevel.Debug:
                    return Microsoft.Extensions.Logging.LogLevel.Debug;

                case NHibernateLogLevel.Info:
                    return Microsoft.Extensions.Logging.LogLevel.Information;

                case NHibernateLogLevel.Warn:
                    return Microsoft.Extensions.Logging.LogLevel.Warning;

                case NHibernateLogLevel.Error:
                    return Microsoft.Extensions.Logging.LogLevel.Error;

                case NHibernateLogLevel.Fatal:
                    return Microsoft.Extensions.Logging.LogLevel.Critical;

                case NHibernateLogLevel.None:
                    return Microsoft.Extensions.Logging.LogLevel.None;

                default:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;
            }
        }
    }
}