using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.SqlServer.DbContexts;
using Sample.SqlServer.Shardings;
using ShardingCore;
using ShardingCore.Sharding.ReadWriteConfigurations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShardingCore.Core;
using ShardingCore.Core.NotSupportShardingProviders;
using ShardingCore.Sharding.ShardingExecutors.Abstractions;
using ShardingCore.TableExists;

namespace Sample.SqlServer
{
    public class Startup
    {
        public static readonly ILoggerFactory efLogger = LoggerFactory.Create(builder =>
        {
            builder.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
        });
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            //services.AddDbContext<DefaultTableDbContext>(o => o.UseSqlServer("Data Source=localhost;Initial Catalog=ShardingCoreDBxx3;Integrated Security=True"));

            services.AddShardingDbContext<DefaultShardingDbContext>()
                .AddEntityConfig(o =>
                {
                    o.CreateShardingTableOnStart = true;
                    o.EnsureCreatedWithOutShardingTable = true;
                    o.AddShardingTableRoute<SysUserModVirtualTableRoute>();
                    o.AddShardingTableRoute<SysUserSalaryVirtualTableRoute>();
                    o.AddShardingTableRoute<TestYearShardingVirtualTableRoute>();
                    o.UseInnerDbContextConfigure(builder =>
                    {
                        builder
                            .ReplaceService<IQuerySqlGeneratorFactory,
                                ShardingSqlServerQuerySqlGeneratorFactory<DefaultShardingDbContext>>()
                            .ReplaceService<IQueryCompiler, NotSupportShardingCompiler>();
                    });
                })
                .AddConfig(op =>
                {
                    op.ConfigId = "c1";
                    op.UseShardingQuery((conStr, builder) =>
                    {
                        builder.UseSqlServer(conStr).UseLoggerFactory(efLogger).ReplaceService<IQuerySqlGeneratorFactory, ShardingSqlServerQuerySqlGeneratorFactory<DefaultShardingDbContext>>();
                    });
                    op.UseShardingTransaction((connection, builder) =>
                    {
                        builder.UseSqlServer(connection).UseLoggerFactory(efLogger).ReplaceService<IQuerySqlGeneratorFactory, ShardingSqlServerQuerySqlGeneratorFactory<DefaultShardingDbContext>>();
                    });
                    op.ReplaceTableEnsureManager(sp => new SqlServerTableEnsureManager<DefaultShardingDbContext>());
                    op.AddDefaultDataSource("A",
                     // "Data Source=localhost;Initial Catalog=ShardingCoreDBXA;Integrated Security=True;"
                     "Data Source = 101.37.117.55;persist security info=True;Initial Catalog=ShardingCoreDBXA;uid=sa;pwd=xjmumixl7610#;Max Pool Size=100;"
                     );
                }).EnsureConfig();
            services.TryAddSingleton<INotSupportShardingProvider, UnionSupportShardingProvider>();
            //services.AddShardingDbContext<DefaultShardingDbContext1>(
            //        (conn, o) =>
            //            o.UseSqlServer(conn).UseLoggerFactory(efLogger)
            //    ).Begin(o =>
            //    {
            //        o.CreateShardingTableOnStart = true;
            //        o.EnsureCreatedWithOutShardingTable = true;
            //        o.AutoTrackEntity = true;
            //        o.MaxQueryConnectionsLimit = Environment.ProcessorCount;
            //        o.ConnectionMode = ConnectionModeEnum.SYSTEM_AUTO;
            //        //if SysTest entity not exists in db and db is exists
            //        //o.AddEntityTryCreateTable<SysTest>(); // or `o.AddEntitiesTryCreateTable(typeof(SysTest));`
            //    })
            //    //.AddShardingQuery((conStr, builder) => builder.UseSqlServer(conStr).UseLoggerFactory(efLogger))//无需添加.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) 并发查询系统会自动添加NoTracking
            //    .AddShardingTransaction((connection, builder) =>
            //        builder.UseSqlServer(connection).UseLoggerFactory(efLogger))
            //    .AddDefaultDataSource("A",
            //        "Data Source=localhost;Initial Catalog=ShardingCoreDBXA;Integrated Security=True;")
            //    .AddShardingTableRoute(o =>
            //    {
            //    }).End();

            services.AddHealthChecks().AddDbContextCheck<DefaultShardingDbContext>();
            //services.AddShardingDbContext<DefaultShardingDbContext, DefaultTableDbContext>(
            //    o => o.UseSqlServer("Data Source=localhost;Initial Catalog=ShardingCoreDB;Integrated Security=True;")
            //    , op =>
            //     {
            //         op.EnsureCreatedWithOutShardingTable = true;
            //         op.CreateShardingTableOnStart = true;
            //         op.UseShardingOptionsBuilder(
            //             (connection, builder) => builder.UseSqlServer(connection).UseLoggerFactory(efLogger),//使用dbconnection创建dbcontext支持事务
            //             (conStr,builder) => builder.UseSqlServer(conStr).UseLoggerFactory(efLogger).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            //                 //.ReplaceService<IQueryTranslationPostprocessorFactory,SqlServer2008QueryTranslationPostprocessorFactory>()//支持sqlserver2008r2
            //                 );//使用链接字符串创建dbcontext
            //         //op.UseReadWriteConfiguration(sp => new List<string>()
            //         //{
            //         //    "Data Source=localhost;Initial Catalog=ShardingCoreDB1;Integrated Security=True;",
            //         //    "Data Source=localhost;Initial Catalog=ShardingCoreDB2;Integrated Security=True;"
            //         //}, ReadStrategyEnum.Random);
            //         op.AddShardingTableRoute<SysUserModVirtualTableRoute>();
            //         op.AddShardingTableRoute<SysUserSalaryVirtualTableRoute>();
            //     });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var startNew = Stopwatch.StartNew();
            startNew.Start();
            app.UseShardingCore();
            startNew.Stop();
            Console.WriteLine($"UseShardingCore:" + startNew.ElapsedMilliseconds + "ms");
            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.DbSeed();
        }
    }

    public class UnionSupportShardingProvider : INotSupportShardingProvider
    {
        public void CheckNotSupportSharding(IQueryCompilerContext queryCompilerContext)
        {
        }

        public bool IsNotSupportSharding(IQueryCompilerContext queryCompilerContext)
        {
            return queryCompilerContext.IsUnion();
        }
    }
}