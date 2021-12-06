﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using ShardingCore.Exceptions;
using ShardingCore.Extensions;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.Enumerators;
using ShardingCore.Sharding.MergeEngines.Common;
using ShardingCore.Sharding.StreamMergeEngines;

namespace ShardingCore.Sharding.MergeEngines.Abstractions.InMemoryMerge
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/17 14:22:10
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    internal abstract class AbstractInMemoryAsyncMergeEngine<TEntity> : AbstractBaseMergeEngine<TEntity>, IInMemoryAsyncMergeEngine<TEntity>
    {
        private readonly MethodCallExpression _methodCallExpression;
        private readonly StreamMergeContext<TEntity> _mergeContext;
        private readonly IQueryable<TEntity> _queryable;
        private readonly Expression _secondExpression;

        public AbstractInMemoryAsyncMergeEngine(MethodCallExpression methodCallExpression, IShardingDbContext shardingDbContext)
        {
            _methodCallExpression = methodCallExpression;
            if (methodCallExpression.Arguments.Count < 1 || methodCallExpression.Arguments.Count > 2)
                throw new ArgumentException($"argument count must 1 or 2 :[{methodCallExpression.ShardingPrint()}]");
            for (int i = 0; i < methodCallExpression.Arguments.Count; i++)
            {
                var expression = methodCallExpression.Arguments[i];
                if (typeof(IQueryable).IsAssignableFrom(expression.Type))
                {
                    if (_queryable != null)
                        throw new ArgumentException(
                            $"argument found more 1 IQueryable :[{methodCallExpression.ShardingPrint()}]");
                    _queryable = new EnumerableQuery<TEntity>(expression);
                }
                else
                {
                    _secondExpression = expression;
                }
            }
            if (_queryable == null)
                throw new ArgumentException($"argument not found IQueryable :[{methodCallExpression.ShardingPrint()}]");
            if (methodCallExpression.Arguments.Count == 2)
            {
                if (_secondExpression == null)
                    throw new ShardingCoreInvalidOperationException(methodCallExpression.ShardingPrint());

                // ReSharper disable once VirtualMemberCallInConstructor
                _queryable = CombineQueryable(_queryable, _secondExpression);
            }


            _mergeContext = ((IStreamMergeContextFactory)ShardingContainer.GetService(typeof(IStreamMergeContextFactory<>).GetGenericType0(shardingDbContext.GetType()))).Create(_queryable, shardingDbContext);
        }
        /// <summary>
        /// 合并queryable
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="secondExpression"></param>
        /// <returns></returns>
        protected abstract IQueryable<TEntity> CombineQueryable(IQueryable<TEntity> queryable, Expression secondExpression);

        private (IQueryable queryable, DbContext dbContext) CreateAsyncExecuteQueryable<TResult>(string dsname, TableRouteResult tableRouteResult, ConnectionModeEnum connectionMode)
        {
            var shardingDbContext = _mergeContext.CreateDbContext(dsname, tableRouteResult, connectionMode);
            var newQueryable = (IQueryable<TEntity>)GetStreamMergeContext().GetReWriteQueryable()
                .ReplaceDbContextQueryable(shardingDbContext);
            var newCombineQueryable = DoCombineQueryable<TResult>(newQueryable);
            return (newCombineQueryable, shardingDbContext);
            ;
        }

        public async Task<List<RouteQueryResult<TResult>>> ExecuteAsync<TResult>(Func<IQueryable, Task<TResult>> efQuery, CancellationToken cancellationToken = new CancellationToken())
        {
            var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
            var waitExecuteQueue = GetDataSourceGroupAndExecutorGroup<RouteQueryResult<TResult>>(true,defaultSqlRouteUnits,
                   async sqlExecutorUnit =>
                    {
                        var connectionMode = _mergeContext.RealConnectionMode(sqlExecutorUnit.ConnectionMode);
                        var dataSourceName = sqlExecutorUnit.RouteUnit.DataSourceName;
                        var routeResult = sqlExecutorUnit.RouteUnit.TableRouteResult;

                        var (asyncExecuteQueryable, dbContext) =
                            CreateAsyncExecuteQueryable<TResult>(dataSourceName, routeResult, connectionMode);

                        var queryResult = await efQuery(asyncExecuteQueryable);
                        var routeQueryResult = new RouteQueryResult<TResult>(dataSourceName, routeResult, queryResult);
                        return new ShardingMergeResult<RouteQueryResult<TResult>>(dbContext, routeQueryResult);
                    }).ToArray();

            return (await Task.WhenAll(waitExecuteQueue)).SelectMany(o => o).ToList();
        }


        ///// <summary>
        ///// 异步并发查询
        ///// </summary>
        ///// <typeparam name="TResult"></typeparam>
        ///// <param name="queryable"></param>
        ///// <param name="dataSourceName"></param>
        ///// <param name="routeResult"></param>
        ///// <param name="efQuery"></param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public async Task<RouteQueryResult<TResult>> AsyncParallelResultExecute<TResult>(IQueryable queryable,string dataSourceName,TableRouteResult routeResult, Func<IQueryable, Task<TResult>> efQuery,
        //    CancellationToken cancellationToken = new CancellationToken())
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var queryResult = await efQuery(queryable);

        //    return new RouteQueryResult<TResult>(dataSourceName, routeResult, queryResult);
        //}

        public virtual IQueryable DoCombineQueryable<TResult>(IQueryable<TEntity> queryable)
        {
            return queryable;
        }

        protected override StreamMergeContext<TEntity> GetStreamMergeContext()
        {
            return _mergeContext;
        }

        //public IQueryable<TEntity> GetQueryable()
        //{
        //    return _queryable;
        //}

        protected MethodCallExpression GetMethodCallExpression()
        {
            return _methodCallExpression;
        }

        protected Expression GetSecondExpression()
        {
            return _secondExpression;
        }
    }
}