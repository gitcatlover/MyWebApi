using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebApi
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
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            //需要从加载配置文件appsettings.json
            services.AddOptions();
            //需要存储速率限制计算器和ip规则
            services.AddMemoryCache();
            //从appsettings.json中加载常规配置，IpRateLimiting与配置文件中节点对应
            services.Configure<ClientRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            //从appsettings.json中加载Ip规则
            services.Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"));
            //注入计数器和规则存储
            services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddControllers();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //配置（解析器、计数器密钥生成器）
            services.AddSingleton<IRateLimitConfiguration,RateLimitConfiguration>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var jwtConfig = Configuration.GetSection("Jwt");
            //生成密钥
            var symmetricKeyAsBase64 = jwtConfig.GetValue<string>("Secret");
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);

            //令牌必须满足这些条件才是合格的，相当于解密过程
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,//是否验证签名
                IssuerSigningKey = signingKey,//解密的密钥
                ValidateIssuer = true,//是否验证发行人
                ValidIssuer = jwtConfig.GetValue<string>("Iss"),//发行人
                ValidateAudience = true,//是否验证订阅人
                ValidAudience = jwtConfig.GetValue<string>("Aud"),//订阅人
                ValidateLifetime = true,//是否验证过期时间，过期了就拒绝访问
                ClockSkew = TimeSpan.FromSeconds(30),
                RequireExpirationTime = true
            };
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = tokenValidationParameters;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyWebApi", Version = "v1" });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference()
                            {
                                Id =  JwtBearerDefaults.AuthenticationScheme,
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Description = "JWT授权(数据将在请求头中进行传输) 参数结构: \"Authorization: Bearer {token}\"",
                    Name = "Authorization", //jwt默认的参数名称
                    In = ParameterLocation.Header, //jwt默认存放Authorization信息的位置(请求头中)
                    Type = SecuritySchemeType.ApiKey
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyWebApi v1"));
            }

            app.UseRouting();

            app.UseAuthentication();//认证
            app.UseAuthorization();//授权
            //启用客户端限制
            app.UseClientRateLimiting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
