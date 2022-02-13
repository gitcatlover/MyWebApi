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
            //��Ҫ�Ӽ��������ļ�appsettings.json
            services.AddOptions();
            //��Ҫ�洢�������Ƽ�������ip����
            services.AddMemoryCache();
            //��appsettings.json�м��س������ã�IpRateLimiting�������ļ��нڵ��Ӧ
            services.Configure<ClientRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            //��appsettings.json�м���Ip����
            services.Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"));
            //ע��������͹���洢
            services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddControllers();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //���ã�����������������Կ��������
            services.AddSingleton<IRateLimitConfiguration,RateLimitConfiguration>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var jwtConfig = Configuration.GetSection("Jwt");
            //������Կ
            var symmetricKeyAsBase64 = jwtConfig.GetValue<string>("Secret");
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);

            //���Ʊ���������Щ�������Ǻϸ�ģ��൱�ڽ��ܹ���
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,//�Ƿ���֤ǩ��
                IssuerSigningKey = signingKey,//���ܵ���Կ
                ValidateIssuer = true,//�Ƿ���֤������
                ValidIssuer = jwtConfig.GetValue<string>("Iss"),//������
                ValidateAudience = true,//�Ƿ���֤������
                ValidAudience = jwtConfig.GetValue<string>("Aud"),//������
                ValidateLifetime = true,//�Ƿ���֤����ʱ�䣬�����˾;ܾ�����
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
                    Description = "JWT��Ȩ(���ݽ�������ͷ�н��д���) �����ṹ: \"Authorization: Bearer {token}\"",
                    Name = "Authorization", //jwtĬ�ϵĲ�������
                    In = ParameterLocation.Header, //jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
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

            app.UseAuthentication();//��֤
            app.UseAuthorization();//��Ȩ
            //���ÿͻ�������
            app.UseClientRateLimiting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
