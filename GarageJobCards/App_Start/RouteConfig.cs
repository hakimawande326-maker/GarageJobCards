using System.Web.Mvc;
using System.Web.Routing;

namespace GarageJobCards
{
 public class RouteConfig
 {
 public static void RegisterRoutes(RouteCollection routes)
 {
 routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

 // Friendly URL for booking (Receptionist/Manager only, redirects to
 // login first if not signed in): /book

 routes.MapRoute(
 name: "Book",
 url: "book",
 defaults: new { controller = "JobCard", action = "Create" }
 );

 routes.MapRoute(
 name: "Default",
 url: "{controller}/{action}/{id}",
 defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
 );
 }
 }
}
