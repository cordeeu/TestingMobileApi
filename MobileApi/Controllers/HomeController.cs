using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MobileApi.Controllers
{
    public class HomeController : Controller
    {
        public Boolean wetlandsKey = false;
        public Boolean woodyKey = false;
        public Boolean flowerKey = false;

        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
           // System.Web.Mvc.Controller
            return View();
        }

        public ActionResult Index2()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        
          [HttpGet]
        public ActionResult ValidateKey(String key)
        {

            /*   WetlandSetting wetS = wetlandDb.WetlandSettings.Where(b => b.name == "Key").FirstOrDefault();
               WoodySetting woodyS = woodyDb.WoodySettings.Where(b => b.name == "Key").FirstOrDefault();
               FlowerSetting flowerS = flowerDb.FlowerSettings.Where(b => b.name == "Key").FirstOrDefault();

               if (wetS != null) 
                   if (wetS.valuetext == key)
                       wetlandsKey = true;

               if (woodyS != null)
                   if (woodyS.valuetext == key)
                       woodyKey = true;

               if (flowerS != null)
                   if (flowerS.valuetext == key)
                       flowerKey = true;

               if (wetlandsKey == true)
               return View("~/Views/Stuff/wetland.cshtml");

               if (woodyKey == true)
                   return View("~/Views/Stuff/woody.cshtml");

               if (flowerKey == true)
                   return View("~/Views/Stuff/flower.cshtml");

               else*/
            return View("~/Views/Home/index.cshtml");
        }

        [HttpGet]
        public ActionResult copySpecificFile(String uploadFile)
        {

            string nameNew = "farts.txt";
            if (uploadFile == null)
            {
               nameNew = "nullCunt.txt";
            }
            else
            {
                nameNew = uploadFile+".txt";
            }
        
            string FileToCopy = null;
            string NewCopy = null;

            FileToCopy = "C:\\Temp\\Farts\\Toots.txt";
            NewCopy = "C:\\Temp\\Toots\\" + nameNew; // Farts.txt";


            if (System.IO.File.Exists(FileToCopy) == true && !System.IO.File.Exists(NewCopy))
            {
                System.IO.File.Copy(FileToCopy, NewCopy);
            }
            else if (System.IO.File.Exists(FileToCopy) == true && System.IO.File.Exists(NewCopy))
            {
                System.IO.File.Delete(NewCopy);
            }

            return null;
            //return View("/Views/Home/index.cshtml");
        }
        [HttpGet]
        public ActionResult UploadFile(FileResult uploadFile)
        {
            string FileToCopy = null;
            string NewCopy = null;


            if (uploadFile == null)
            {
                FileToCopy = "C:\\Temp\\Farts\\Toots.txt";
                NewCopy = "C:\\Temp\\Toots\\NullFarts.txt";

            }
            else if (uploadFile.FileDownloadName.Equals(""))
            {
                FileToCopy = "C:\\Temp\\Farts\\Toots.txt";
                NewCopy = "C:\\Temp\\Toots\\BlankFarts.txt";
            }
            else
            {
                FileToCopy = uploadFile.FileDownloadName;
                NewCopy = "C:\\Temp\\Toots\\Darts.txt";
            }

            if (System.IO.File.Exists(FileToCopy) == true && !System.IO.File.Exists(NewCopy))
            {
                System.IO.File.Copy(FileToCopy, NewCopy);
            }
            else if (System.IO.File.Exists(FileToCopy) == true && System.IO.File.Exists(NewCopy))
            {
                System.IO.File.Delete(NewCopy);
            }

            return null;
            //return View("/Views/Home/index.cshtml");
        }
    }
}
