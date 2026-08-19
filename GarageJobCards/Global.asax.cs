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

 // Recreates the DB when the model changes; swap for a Migrations-based
 // initializer (or remove entirely) once you're past active development.
 Database.SetInitializer(new GarageContextInitializer());
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
