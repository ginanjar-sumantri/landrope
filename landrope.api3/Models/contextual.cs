﻿using HttpAccessor;
using landrope.mod;
using landrope.mod2;
using landrope.mod3;
using landrope.mod4;
using landrope.mod.cross;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using auth.mod;
using mongospace;
using System;
using Tracer;

namespace landrope.api3
{
    internal static class Contextual
    {
        public static LandropeContext GetContext()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<LandropeContext>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }
        public static ExtLandropeContext GetContextExt()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<ExtLandropeContext>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }

        public static LandropePlusContext GetContextPlus()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<LandropePlusContext>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }

        public static LandropePayContext GetContextPay()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<LandropePayContext>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }

        public static authEntities GetAuthContext()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<authEntities>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }

        public static LandropeCrossContext GetContextCross()
        {
            try
            {
                return HttpAccessor.Helper.HttpContext.RequestServices.GetService<LandropeCrossContext>();
            }
            catch (Exception ex)
            {
                MyTracer.TraceError2(ex);
                throw;
            }
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'LandropContextProvider'
    public class LandropContextProvider : IServiceProvider
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'LandropContextProvider'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'LandropContextProvider.GetService(Type)'
        public object GetService(Type serviceType)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'LandropContextProvider.GetService(Type)'
        {
            return new LandropeContext();
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'ExtLandropContextProvider'
    public class ExtLandropContextProvider : IServiceProvider
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'ExtLandropContextProvider'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'ExtLandropContextProvider.GetService(Type)'
        public object GetService(Type serviceType)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'ExtLandropContextProvider.GetService(Type)'
        {
            return new ExtLandropeContext();
        }
    }
}