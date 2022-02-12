using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace MyWebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class JWTController : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IConfiguration _configuration;
        //依赖注入是实现控制反转的一种方式
        public JWTController(IHttpContextAccessor accessor, IConfiguration configuration)
        {
            //依赖注入的HttpContextAccessor，先运行HttpContextAccessor的构造函数，然后再运行控制器的构造函数,
            //HttpContextAccessor的控制权反转到了控制器的调用者
            this._accessor = accessor;
            this._configuration = configuration;
        }
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            //用户定义
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Name,"MrFeng"),
                new Claim(JwtRegisteredClaimNames.Email,"test.live.com"),
                new Claim(JwtRegisteredClaimNames.Sub,"uid")
            };

            //服务器定义
            var jwtConfig = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtConfig.GetValue<string>("Secret")));
            var token = new JwtSecurityToken(
               issuer: jwtConfig.GetValue<string>("Iss"),
               audience: jwtConfig.GetValue<string>("Aud"),
               claims: claims,
               expires: DateTime.Now.AddHours(1),
               signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return new string[] { jwtToken };
        }

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<string>> Get2([FromQuery] string jwtStr)
        {
            //1
            //解析GET中令牌数据
            var jwtHadler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHadler.ReadJwtToken(jwtStr);

            //2
            var name = _accessor.HttpContext.User.Identity.Name;
            var claims = _accessor.HttpContext.User.Claims;
            var claimTypeVal = (from item in claims
                                where item.Type == JwtRegisteredClaimNames.Email
                                select item.Value).ToList();
            return new string[] { JsonConvert.SerializeObject(jwtToken), name, JsonConvert.SerializeObject(claimTypeVal) }; ;
        }
    }
}
