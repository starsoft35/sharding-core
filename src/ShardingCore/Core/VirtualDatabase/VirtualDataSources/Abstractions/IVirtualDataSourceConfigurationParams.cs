﻿using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.ReadWriteConfigurations;
using ShardingCore.Sharding.ShardingComparision.Abstractions;
using System.Collections.Generic;
using System.Data.Common;
using ShardingCore.TableExists.Abstractions;

namespace ShardingCore.Core.VirtualDatabase.VirtualDataSources.Abstractions
{
    public interface IVirtualDataSourceConfigurationParams
    {
        /// <summary>
        /// 配置id
        /// </summary>
        string ConfigId { get; }
        /// <summary>
        /// 优先级
        /// </summary>
        int Priority { get; }
        /// <summary>
        /// 不能小于等于0 should greater than or equal  zero
        /// </summary>
        int MaxQueryConnectionsLimit { get; }
        /// <summary>
        /// 连接模式,如果没有什么特殊情况请是用系统自动 默认<code>ConnectionModeEnum.SYSTEM_AUTO</code>
        /// </summary>
        ConnectionModeEnum ConnectionMode { get; }

        /// <summary>
        /// 默认数据源
        /// </summary>
        string DefaultDataSourceName { get; }
        /// <summary>
        /// 默认数据源链接字符串
        /// </summary>
        string DefaultConnectionString { get; }
        /// <summary>
        /// 不能为空null,should not null
        /// </summary>
        IDictionary<string,string> ExtraDataSources { get; }
        /// <summary>
        /// null表示不启用读写分离,if null mean not enable read write
        /// </summary>
        IDictionary<string, IEnumerable<string>> ReadWriteSeparationConfigs { get; }

        ReadStrategyEnum? ReadStrategy { get; }
        bool? ReadWriteDefaultEnable { get; }
        int? ReadWriteDefaultPriority { get; }
        /// <summary>
        /// 读写分离链接字符串获取
        /// </summary>
        ReadConnStringGetStrategyEnum? ReadConnStringGetStrategy { get; }
        /// <summary>
        /// 不能为空 should not null
        /// </summary>
        IShardingComparer ShardingComparer { get; }
        /// <summary>
        /// 表确认管理者
        /// </summary>
        ITableEnsureManager TableEnsureManager { get; }
        /// <summary>
        /// 如何根据connectionString 配置 DbContextOptionsBuilder
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsBuilder"></param>
        /// <returns></returns>
        DbContextOptionsBuilder UseDbContextOptionsBuilder(string connectionString, DbContextOptionsBuilder dbContextOptionsBuilder);
        /// <summary>
        /// 如何根据dbConnection 配置DbContextOptionsBuilder
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="dbContextOptionsBuilder"></param>
        /// <returns></returns>
        DbContextOptionsBuilder UseDbContextOptionsBuilder(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder);
        /// <summary>
        /// 真实DbContextOptionBuilder的配置
        /// </summary>
        /// <param name="dbContextOptionsBuilder"></param>
        void UseInnerDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder);
        /// <summary>
        /// 使用读写分离
        /// </summary>
        /// <returns></returns>
        bool UseReadWriteSeparation();
    }

    public interface IVirtualDataSourceConfigurationParams<TShardingDbContext> : IVirtualDataSourceConfigurationParams
        where TShardingDbContext : DbContext, IShardingDbContext
    {

    }
}
