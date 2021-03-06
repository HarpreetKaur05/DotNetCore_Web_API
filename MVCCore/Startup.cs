using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using System;
using Lamar;
using MVCCore.StructureMap;
using MediatR;
using System.Reflection;
using MVCCore.BL;
using MVCCore.Mediator.Request;
using FluentValidation;
using FluentValidation.AspNetCore;
using MVCCore.Mediator.Handler.ValidatorHandler;
using MVCCore.Models;
using AppContext = MVCCore.Models.AppContext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace MVCCore
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
            var container = new Lamar.Container(x =>
            { 
                services.AddScoped<IMessagingService, StructureMappingService>();
                services.AddControllersWithViews();
                services.AddMvc()               
                        .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<LoginValidatorHandler>())
                        .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RegisterValidationHandler>())
                        .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ChangePasswordValidator>());   
                services.AddDbContext<AppContext>(options =>
                                options.UseSqlServer(Configuration.GetConnectionString("DevConnection"),b => b.MigrationsAssembly("MVCCore")));
                
                services.AddDistributedMemoryCache();  
                services.AddSession();

                services.AddMediatR(typeof(Startup));
                services.AddMediatR(Assembly.GetExecutingAssembly()); 

                services.AddIdentity<IdentityUser, IdentityRole>()
                        .AddEntityFrameworkStores<AppContext>();
                
                services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3);
                services.ConfigureApplicationCookie(options => options.LoginPath = "/login");
            });                     
        }

        public void ConfigureContainer(ServiceRegistry services)
        {
            services.Scan(s =>
            {
                s.TheCallingAssembly();
                s.WithDefaultConventions();
                s.AssembliesAndExecutablesFromApplicationBaseDirectory(assembly => assembly.GetName().Name.StartsWith("MVCCore"));
                s.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));    

            });

        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseSession();
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();  

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Login}/{id?}");
            });
        }
    }
}
