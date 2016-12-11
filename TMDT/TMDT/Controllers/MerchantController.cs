using ImageResizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TMDT.DAO;
using TMDT.Others;
namespace TMDT.Controllers
{
    public class MerchantController : Controller
    {


        public List<HttpPostedFileBase> fileUpload = new List<HttpPostedFileBase>();
        
        [HttpGet]
        public ActionResult Index()
        {
            var user = Session["User"] as TMDT.Account;
            if (user == null)
                return RedirectToAction("Login", "Home");
            else
            {
                
                if (user.Level == 2)
                    return RedirectToAction("Home", "Upgrade");
                else
                {
                    var model = new ProductDAO().ListAll(user.AccountID);
                    foreach (var item in model)
                    {
                        int i = item.Image.IndexOf("|");
                        item.Image = item.Image.Substring(0, i);
                    }
                    return View(model);
                }

            }                
            
        }

        [HttpGet]
        public ActionResult Sell()
        {
            var a=new AccountDAO();
            Account user = Session["User"] as Account;
            if (user != null)
            {
                if (user.Level == 2 || a.CountDayLeft(user.AccountID).Days == 0 )
                {
                    return RedirectToAction("Upgrade", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Categories = new CategoryDAO().DropdownCategory();
            //SetViewBag();
            //ViewBag.CategoryID = new SelectList(new CategoryDAO().ListAll(), "CategoryID", "CategoryName");
            return View();
        }

        public void SetViewBag(int? selectedID = null)
        {
            var dao = new CategoryDAO();
            ViewBag.CategoryID = new SelectList(dao.ListAll(), "CategoryID", "CategoryName");
        }

        public ActionResult SaveUploadedFile()
        {
            bool isSavedSuccessfully = true;
            string fName = "";
            //int counta = Request.Files.Count;
            foreach (string fileName in Request.Files)
            {
                HttpPostedFileBase file = Request.Files[fileName];
                fName = file.FileName;
                var fileName1 = Path.GetFileName(file.FileName);
                if (file != null && file.ContentLength > 0)
                {
                    //model.fileUpload.Add(file);
                    if (Session["fileUpload"] == null)
                    {
                        fileUpload.Add(file);

                    }
                    else
                    {
                        fileUpload = (List<HttpPostedFileBase>)Session["fileUpload"];
                        fileUpload.Add(file);

                    }
                }
            }
            Session["fileUpload"] = fileUpload;


            if (isSavedSuccessfully)
            {
                return Json(new { Message = fName });
            }
            else
            {
                return Json(new { Message = "Error in saving file" });
            }
        }

        [HttpPost]
        public ActionResult DeleteFile(string id)
        {

            fileUpload = (List<HttpPostedFileBase>)Session["fileUpload"];

            Session.Remove("fileUpload");
            bool isSavedSuccessfully = true;
            string fName = "";
            foreach (var file in fileUpload)
            {
                if (file.FileName == id)
                {
                    fileUpload.Remove(file);
                    break;
                }
            }
            Session["fileUpload"] = fileUpload;
            if (isSavedSuccessfully)
            {
                return Json(new { Message = fName });
            }
            else
            {
                return Json(new { Message = "Error in delete file" });
            }
        }

        [HttpPost]
        public ActionResult Sell(Product model)
        {

            if (ModelState.IsValid && Session["fileUpload"] != null)
            {
                var user = Session["User"] as TMDT.Account;
                model.AccountID = user.AccountID;
                model.CreatedDate = DateTime.Now;

                string _fileName;
                string listImages = "";
                //if (Session["fileUpload"] != null)
                //{
                    fileUpload = (List<HttpPostedFileBase>)Session["fileUpload"];
                    foreach (var item in fileUpload)
                    {
                        //if (item != null && item.ContentLength > 0)
                        // {
                        string pic = Path.GetFileName(item.FileName).Substring(0, Path.GetFileName(item.FileName).IndexOf("."));
                        _fileName = user.UserName + "_" + pic + ".jpg";
                        var path = Path.Combine(Server.MapPath("~/public/MyImages/"), _fileName);
                        item.SaveAs(path);
                        //ImageJob i1 = new ImageJob(item, path, new Instructions("format=jpg"), false, true);
                        //i1.Build();

                        //upload to Imgur
                        listImages += _fileName + "|";



                    }
                //}
                model.Image = listImages;
                var dao = new ProductDAO();
                dao.Create(model);
                Session["fileUpload"] = null;
                ViewBag.Categories = new CategoryDAO().DropdownCategory();
                //SetViewBag();
                //ViewBag.CategoryID = new SelectList(new CategoryDAO().ListAll(), "CategoryID", "CategoryName");
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Message = "Đăng sản phẩm thất bại. Vui lòng nhập lại.";
                ViewBag.Categories= new CategoryDAO().DropdownCategory();
            }
            return View(model);

        }


        public ActionResult ProductInfo(int id)
        {
            Product product = new ProductDAO().CTSP(id);
            return View(product);
        }

        [HttpGet]
        public ActionResult ChangeInfo(int? id)
        {
            var user = Session["User"] as TMDT.Account;
            if (user== null || user.Level!=1)
            {
                return RedirectToAction("Login", "Home");
            }
            var model = new AccountDAO().GetUser_ID(id);
            return View(model);
        }

        [HttpPost]
        public ActionResult ChangeInfo(Account acc)
        {
            if (acc.Pass != null)
            {
                TempData["Message"] = "Cập nhật mật khẩu thành công.";
                
                acc.Pass = Encryptor.MD5Hash(acc.Pass);
            }
                
            else
            {
                TempData["Message"] = "Cập nhật thông tin tài khoản thành công.";
                
            }
                
            new AccountDAO().UpdateAcc(acc);
            
            return RedirectToAction("AccountInfo", "Home", new { id = acc.AccountID });

        }
    }
}
