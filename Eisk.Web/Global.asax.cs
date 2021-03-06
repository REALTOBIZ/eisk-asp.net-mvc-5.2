﻿using Eisk.DataAccess;
using Eisk.Helpers;
using Eisk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
namespace Eisk.Web.Global
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RouteConfig.RegisterRoutes(RouteTable.Routes);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            
            DataInitializer.Init();
            DisplayModelInitializer.Init();
            DependencyInjectorInitializer.Init();
            
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {

            if (HttpContext.Current.User != null)
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {

                    if (HttpContext.Current.User.Identity.AuthenticationType != "Forms")
                        throw new InvalidOperationException("Only forms authentication is supported, not " +
                            HttpContext.Current.User.Identity.AuthenticationType);

                    System.Security.Principal.IIdentity userId = HttpContext.Current.User.Identity;

                    //if role info is already NOT loaded into cache, put the role info in cache
                    if (HttpContext.Current.Cache[userId.Name] == null)
                    {
                        string[] roles = new string[1] { GetUserRoleByUserName(userId.Name) };

                        //1 hour sliding expiring time. Adding the roles in chache. This will be used in Application_AuthenticateRequest event located in Global.ascx.cs file to attach user Principal object.
                        HttpContext.Current.Cache.Add(userId.Name, roles, null, DateTime.MaxValue, TimeSpan.FromHours(1), System.Web.Caching.CacheItemPriority.BelowNormal, null);
                    }

                    //now assign the user role in the current security context
                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal
                        (userId, (string[])System.Web.HttpContext.Current.Cache[userId.Name]);
                }
            }

        }

        public override void Init()
        {
            //this.PostAuthenticateRequest += this.PostAuthenticateRequestHandler;
            this.EndRequest += this.EndRequestHandler;

            base.Init();
        }

        private void EndRequestHandler(object sender, EventArgs e)
        {
            // This is a workaround since subscribing to HttpContext.Current.ApplicationInstance.EndRequest 
            // from HttpContext.Current.ApplicationInstance.BeginRequest does not work. 
            IEnumerable<UnityHttpContextPerRequestLifetimeManager> perRequestManagers =
                DependencyHelper.Container.Registrations
                    .Select(r => r.LifetimeManager)
                    .OfType<UnityHttpContextPerRequestLifetimeManager>()
                    .ToArray();

            foreach (var manager in perRequestManagers)
            {
                manager.Dispose();
            }
        }

        #region Utilities
        
        //this info will typically be loaded from database
        private string GetUserRoleByUserName(string userName)
        {
            var user = UserDataAccess.GetByUserName(userName);
            return user == null ? string.Empty : user.UserRole;
        }

        #endregion
    }
}