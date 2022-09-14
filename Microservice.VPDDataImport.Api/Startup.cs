using FluentValidation;
using Microservice.VPDDataImport.Persistence.Database;
using Microservice.VPDDataImport.Services.EventHandlers;
using Microservice.VPDDataImport.Services.EventHandlers.Commands;
using Microservice.VPDDataImport.Services.EventHandlers.Validators;
using Microservice.VPDDataImport.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Api
{
    public class Startup
    {
        private  readonly string _MyCors = "Cors";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VPD.DataImport.Microservice", Version = "v1" });
            });
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("ConexionDatabase"));
            });
            services.AddCors(options =>
            {
                options.AddPolicy(name: _MyCors, builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            services.AddMediatR(Assembly.Load("VPDDataImport.Services.EventHandlers"));                                           
            services.AddTransient<IhistoryQueryService, historyQueryService>();
            services.AddTransient<IValidator<historyCreateCommand>, historyCreateValidator>();
            services.AddTransient<IDhisQueryService, DhisQueryService>();
            services.AddTransient<ILoginQueryService, LoginQueryService>();

            services.AddHostedService<BackgroundTask>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VPD.DataImport.Microservice v1"));
            }

            app.UseRouting();
            app.UseCors(_MyCors);
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}