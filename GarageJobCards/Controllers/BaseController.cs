using System;
using System.Web.Mvc;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
 // Abstract - MVC ignores this for routing. Inherit from it to get
 // easy access to who's currently logged in via session.
 public abstract class BaseController : Controller
 {
 protected int? CurrentUserId
 {
 get
 {
 var val = Session["UserId"];
 return val == null ? (int?)null : Convert.ToInt32(val);
 }
 }

 protected string CurrentUserName
 {
 get { return Session["UserName"] as string; }
 }

 protected UserRole? CurrentUserRole
 {
 get
 {
 var val = Session["UserRole"] as string;
 UserRole role;
 return (val != null && Enum.TryParse(val, out role)) ? role : (UserRole?)null;
 }
 }

 protected bool IsLoggedIn
 {
 get { return CurrentUserId.HasValue; }
 }
 }
}
