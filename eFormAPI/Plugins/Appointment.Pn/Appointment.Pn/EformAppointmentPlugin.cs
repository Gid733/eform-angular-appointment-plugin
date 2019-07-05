/*
The MIT License (MIT)

Copyright (c) 2007 - 2019 Microting A/S

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Appointment.Pn.Abstractions;
using Appointment.Pn.Infrastructure.Data.Seed;
using Appointment.Pn.Infrastructure.Data.Seed.Data;
using Appointment.Pn.Infrastructure.Models;
using Appointment.Pn.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microting.AppointmentBase.Infrastructure.Data;
using Microting.AppointmentBase.Infrastructure.Data.Factories;
using Microting.eFormApi.BasePn;
using Microting.eFormApi.BasePn.Infrastructure.Database.Extensions;
using Microting.eFormApi.BasePn.Infrastructure.Models.Application;
using Microting.eFormApi.BasePn.Infrastructure.Settings;

namespace Appointment.Pn
{
    public class EformAppointmentPlugin : IEformPlugin
    {
       
        public string Name => "Microting Appointment Plugin";
        public string PluginId => "eform-angular-appointment-plugin";
        public string PluginPath => PluginAssembly().Location;
        private string _connectionString;
        private int _maxParallelism = 1;
        private int _numberOfWorkers = 1;
        
        public Assembly PluginAssembly()
        {
            return typeof(EformAppointmentPlugin).GetTypeInfo().Assembly;
        }

        public void Configure(IApplicationBuilder appBuilder)
        {
            // Do nothing
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAppointmentLocalizationService, AppointmentLocalizationService>();
            services.AddTransient<IAppointmentPnSettingsService, AppointmentPnSettingsService>();
        }

        public void ConfigureOptionsServices(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigurePluginDbOptions<AppointmentBaseSettings>(
                configuration.GetSection("AppointmentBaseSettings"));
        }

        public void ConfigureDbContext(IServiceCollection services, string connectionString)
        {
            _connectionString = connectionString;
            if (connectionString.ToLower().Contains("convert zero datetime"))
            {
                services.AddDbContext<AppointmentPnDbContext>(o => o.UseMySql(connectionString,
                    b => b.MigrationsAssembly(PluginAssembly().FullName)));
            }
            else
            {
                services.AddDbContext<AppointmentPnDbContext>(o => o.UseSqlServer(connectionString,
                    b => b.MigrationsAssembly(PluginAssembly().FullName)));
            }

            AppointmentPnContextFactory contextFactory = new AppointmentPnContextFactory();
            var context = contextFactory.CreateDbContext(new[] {connectionString});
            context.Database.Migrate();

            // Seed database
            SeedDatabase(connectionString);

            string temp = context.PluginConfigurationValues
                .SingleOrDefault(x => x.Name == "AppointmentBaseSettings:MaxParallelism")?.Value;
            _maxParallelism = string.IsNullOrEmpty(temp) ? 1 : int.Parse(temp);

            temp = context.PluginConfigurationValues
                .SingleOrDefault(x => x.Name == "AppointmentBaseSettings:NumberOfWorkers")?.Value;
            _numberOfWorkers = string.IsNullOrEmpty(temp) ? 1 : int.Parse(temp);
        }

        public MenuModel HeaderMenu(IServiceProvider serviceProvider)
        {
            var localizationService = serviceProvider
                .GetService<IAppointmentLocalizationService>();

            var result = new MenuModel();
            result.LeftMenu.Add(new MenuItemModel()
            {
                Name = localizationService.GetString("Appointment"),
                E2EId = "",
                Link = "",
                MenuItems = new List<MenuItemModel>()
                {
                    new MenuItemModel()
                    {
                        Name = localizationService.GetString("Settings"),
                        E2EId = "appointment-pn-settings",
                        Link = "/plugins/appointment-pn/settings",
                        Position = 0,
                    }
                }
            });
            return result;
        }

        public void SeedDatabase(string connectionString)
        {
            var contextFactory = new AppointmentPnContextFactory();
            using (var context = contextFactory.CreateDbContext(new []{connectionString}))
            {
                AppointmentPluginSeed.SeedData(context);
            }
        }

        public void AddPluginConfig(IConfigurationBuilder builder, string connectionString)
        {
            var seedData = new AppointmentConfigurationSeedData();
            var contextFactory = new AppointmentPnContextFactory();
            builder.AddPluginConfiguration(
                connectionString, 
                seedData, 
                contextFactory);
        }
    }
}