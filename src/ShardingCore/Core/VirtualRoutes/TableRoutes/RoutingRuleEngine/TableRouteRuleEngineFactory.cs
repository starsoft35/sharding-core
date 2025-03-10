using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core.VirtualDatabase.VirtualTables;
using ShardingCore.Core.VirtualTables;
using ShardingCore.Sharding.Abstractions;

namespace ShardingCore.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: Thursday, 28 January 2021 13:31:06
    * @Email: 326308290@qq.com
    */
    /// <summary>
    /// 表路由规则引擎工厂
    /// </summary>
    public class TableRouteRuleEngineFactory<TShardingDbContext> : ITableRouteRuleEngineFactory<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
    {
        private readonly ITableRouteRuleEngine<TShardingDbContext> _tableRouteRuleEngine;

        public TableRouteRuleEngineFactory(ITableRouteRuleEngine<TShardingDbContext> tableRouteRuleEngine)
        {
            _tableRouteRuleEngine = tableRouteRuleEngine;
        }
        /// <summary>
        /// 创建表路由上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        public TableRouteRuleContext CreateContext(IQueryable queryable)
        {
            return new TableRouteRuleContext(queryable);
        }

        public IEnumerable<TableRouteResult> Route(IQueryable queryable)
        {
            var ruleContext = CreateContext(queryable);
            return Route(ruleContext);
        }

        public IEnumerable<TableRouteResult> Route(TableRouteRuleContext ruleContext)
        {
            return _tableRouteRuleEngine.Route(ruleContext);
        }
    }
}