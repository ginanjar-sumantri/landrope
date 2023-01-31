using auth.mod;
using landrope.mod.shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace landrope.api3
{
    public static class SecurityExtensions
    {
        public static User ToUser(this user usr) =>
            new User { key = usr.key, FullName = usr.FullName, identifier = usr.identifier, invalid = usr.invalid };

        public static Role ToRole(this role rol) =>
            new Role { key = rol.key, invalid = rol.invalid, description = rol.description };

        public static landrope.mod.shared.Action ToAction(this action act) =>
            new landrope.mod.shared.Action { key = act.key, invalid = act.invalid, identifier = act.identifier, description = act.description };
    }

    public static class ControllerContextExtensions
    {
        public static IWebHostEnvironment GetEnvironment(this ControllerContext ctx)
            => ctx.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

        public static string GetContentRootPath(this ControllerContext ctx)
            => ctx.HttpContext.RequestServices.GetService<IWebHostEnvironment>().ContentRootPath;
    }
}
