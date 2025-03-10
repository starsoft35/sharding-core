using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShardingCore.Core.ShardingConfigurations.Abstractions;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Exceptions;
using ShardingCore.Extensions;
using ShardingCore.Sharding.Abstractions;
using System;
using System.Threading;

namespace ShardingCore.TableCreator
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: Monday, 21 December 2020 11:23:22
    * @Email: 326308290@qq.com
    */
    public class ShardingTableCreator<TShardingDbContext> : IShardingTableCreator<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
    {
        private readonly ILogger<ShardingTableCreator<TShardingDbContext>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShardingEntityConfigOptions<TShardingDbContext> _entityConfigOptions;
        private readonly IRouteTailFactory _routeTailFactory;

        public ShardingTableCreator(ILogger<ShardingTableCreator<TShardingDbContext>> logger,  IServiceProvider serviceProvider, IShardingEntityConfigOptions<TShardingDbContext> entityConfigOptions, IRouteTailFactory routeTailFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _entityConfigOptions = entityConfigOptions;
            _routeTailFactory = routeTailFactory;
        }

        public void CreateTable<T>(string dataSourceName, string tail) where T : class
        {
            CreateTable(dataSourceName, typeof(T), tail);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSourceName"></param>
        /// <param name="shardingEntityType"></param>
        /// <param name="tail"></param>
        public void CreateTable(string dataSourceName, Type shardingEntityType, string tail)
        {
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<TShardingDbContext>();
                var shardingDbContext = (IShardingDbContext)dbContext;
                var context = shardingDbContext.GetDbContext(dataSourceName, false, _routeTailFactory.Create(tail));

                var modelCacheSyncObject = context.GetModelCacheSyncObject();

                var acquire = Monitor.TryEnter(modelCacheSyncObject,TimeSpan.FromSeconds(3));
                if (!acquire)
                {
                    throw new ShardingCoreException("cant get modelCacheSyncObject lock");
                }

                try
                {
                    context.RemoveDbContextRelationModelSaveOnlyThatIsNamedType(shardingEntityType);
                    var databaseCreator = context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
                    try
                    {
                        databaseCreator.CreateTables();
                    }
                    catch (Exception ex)
                    {
                        if (!_entityConfigOptions.IgnoreCreateTableError.GetValueOrDefault())
                        {
                            _logger.LogWarning(ex,
                                $"create table error entity name:[{shardingEntityType.Name}].");
                            throw new ShardingCoreException($" create table error :{ex.Message}", ex);
                        }
                    }
                    finally
                    {
                        context.RemoveModelCache();
                    }
                }
                finally
                {
                    Monitor.Exit(modelCacheSyncObject);
                }

            }
        }
    }
}