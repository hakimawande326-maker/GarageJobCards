using System;
using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Models;

namespace GarageJobCards.Infrastructure
{
 // Usage: [RequireRole(UserRole.Receptionist, UserRole.Manager)]
 // Put [RequireRole] with no arguments to just require "any logged-in user".
 public class RequireRoleAttribute : ActionFilterAttribute
 {
 private readonly UserRole[] allowedRoles;

 public RequireRoleAttribute(params UserRole[] roles)
 {
 allowedRoles = roles;
 }

 public override void OnActionExecuting(ActionExecutingContext filterContext)
 {
 var session = filterContext.HttpContext.Session;
 var userId = session["UserId"];

 if (userId == null)
 {
 var returnUrl = filterContext.HttpContext.Request.RawUrl;
 filterContext.Result = new RedirectResult("~/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
 return;
 }

 if (allowedRoles != null && allowedRoles.Length > 0)
 {
 var roleString = (string)session["UserRole"];
 UserRole currentRole;
 if (!Enum.TryParse(roleString, out currentRole) || !allowedRoles.Contains(currentRole))
 {
 filterContext.Result = new RedirectResult("~/Account/AccessDenied");
 return;
 }
 }

 base.OnActionExecuting(filterContext);
 }
 }
}
