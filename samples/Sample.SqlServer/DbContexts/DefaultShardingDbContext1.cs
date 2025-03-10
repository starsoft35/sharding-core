﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sample.SqlServer.Domain.Maps;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Sharding;
using ShardingCore.Sharding.Abstractions;

namespace Sample.SqlServer.DbContexts
{
    public class DefaultShardingDbContext1:AbstractShardingDbContext, IShardingTableDbContext
    {
        public DefaultShardingDbContext1(DbContextOptions<DefaultShardingDbContext1> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public IRouteTail RouteTail { get; set; }
    }
}
