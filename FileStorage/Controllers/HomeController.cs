using System;
using System.Text;
using System.Web.Mvc;
using FileStorage.Models;

namespace FileStorage.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult File1()
        {
            return new FilePathResult("~/App_Data/file.pdf", "application/pdf")
            {
                FileDownloadName = "file.pdf"
            };
        }

        [HttpPost]
        public ActionResult File(FileQuery model)
        {
            if (model.Login.ToLower() == "pdf")
            {
                return new FilePathResult("~/App_Data/file1.pdf", "application/pdf")
                {
                    FileDownloadName = "file1.pdf"
                };
            } 
            if (model.Login.ToLower() == "error")
            {
                return new ContentResult()
                {
                    Content = string.Format("<xml><query>{0}</query></xml>", model.Login),
                    ContentEncoding = Encoding.UTF8,
                    ContentType = "application/xml"
                };
            }

            return new HttpStatusCodeResult(500, "Can't process request");
        }
    }
}