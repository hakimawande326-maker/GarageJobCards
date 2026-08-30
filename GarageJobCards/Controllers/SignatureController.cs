using System.Linq;
using System.Web.Mvc;
using GarageJobCards.Infrastructure;
using GarageJobCards.Models;

namespace GarageJobCards.Controllers
{
    [RequireRole(UserRole.Manager)]
    public class SignatureController : BaseController
    {
        private readonly GarageContext db = new GarageContext();

        // GET: /Signature
        public ActionResult Index()
        {
            var user = db.Users.Find(CurrentUserId);
            return View(user);
        }

        // POST: /Signature
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string signatureDataUrl)
        {
            var user = db.Users.Find(CurrentUserId);

            if (!string.IsNullOrEmpty(signatureDataUrl))
            {
                user.SignatureImageDataUrl = signatureDataUrl;
                db.SaveChanges();
                TempData["Success"] = "Signature saved.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
