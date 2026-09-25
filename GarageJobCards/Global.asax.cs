using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using GarageJobCards.Models;

namespace GarageJobCards
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Uses EF Migrations to keep the database schema up to date -
            // applies any pending migration automatically on startup,
            // without ever dropping/recreating the database like the old
            // GarageContextInitializer did. Fully-qualified "Configuration"
            // here because .NET Framework's own System.Configuration.Configuration
            // class would otherwise be picked instead of ours.
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<GarageContext, GarageJobCards.Models.Configuration>());
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}