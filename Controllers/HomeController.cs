using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DSTBuilder.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Oracle()
        {
            ViewBag.Message = "Oracle Only Builds";
            return View();
        }

        public ActionResult Deploy()
        {
            ViewBag.Message = "Push everything to Egynte";
            return View();
        }
    }
}
