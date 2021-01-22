using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

namespace EventStoreDbShowProjection
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "EventStoreDbShowProjection", Version = "v1"});
            });

            var mongo = CreateMongoClient();
            var database = mongo.GetDatabase("ESShow");
            var checkpointCollection = database.GetCollection<Checkpoint>(nameof(Checkpoint));
            var tryGetCheckpoint = MongoCheckpointStore.PrepareTryGetCheckpoint(checkpointCollection);
            var saveCheckpoint = MongoCheckpointStore.PrepareSaveStreamCheckpoint(checkpointCollection);

            services.AddSingleton<IHostedService>(
                new OrderSubscription(
                    database.GetCollection<Order>(nameof(Order)),
                    CreateEsClient(),
                    tryGetCheckpoint,
                    saveCheckpoint)
            );

            EventStoreClient CreateEsClient()
            {
                var settings = EventStoreClientSettings
                    .Create(Configuration["EventStore:ConnectionString"]);
                return new EventStoreClient(settings);
            }
            
            MongoClient CreateMongoClient()
                => new(Configuration["Mongo:ConnectionString"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventStoreDbShowProjection v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}