using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TMDT.DAO;
using TMDT.Others;
using TMDT.Models;
using System.Data.Entity;
using PagedList;
using System.Data.Entity.Validation;
using System.Diagnostics;
using TMDT.Logic;

namespace TMDT.Controllers
{
    public class HomeController : Controller
    {
       
        public ActionResult Index()
        {
            ViewBag.New = new ProductDAO().ListNewProduct(4);
            ViewBag.Category = new ProductDAO().ListAllCat();
            return View(new ProductDAO().ListAll());

        }
        public ActionResult GetData()
        {
            TMDTModel db = new TMDTModel();
            var ListCategory = db.Categories.ToList();
            SelectList list = new SelectList(ListCategory, "CategoryID", "CategoryName");
            ViewBag.CategoryList = list;
            return PartialView(ViewBag.CategoryList);
           // Of whatever you need to return.
        }
        public ActionResult Details(int id)
        {
            TMDTModel db = new TMDTModel();
            //Product pro = db.Products.Find(id);
            //string temp = pro.Image;
            //string[] image = new string[10];
            //int m = 0;
            //for (int i = 0;i<8;i++)
            //{
            //    image[i] = temp.Substring(m, temp.IndexOf("|"));
            //    m = temp.IndexOf("|");
            //    temp = temp.Substring(temp.IndexOf("|"));
            //    if (temp.IndexOf("|").Equals(null)) break;
            //}

            return View(db.Products.Find(id));
        }

        public ActionResult ReturnSearch(string name, string category, string pricetext1, string pricetext2, int? page, string currentFilter)
        {
            TMDTModel db = new TMDTModel();
            db.Products.Load();
            var product = db.Products.Include(s => s.Category);
            if (name != null)
            {
                page = 1;
            }
            else
            {
                name = currentFilter;
            }

            ViewBag.CurrentFilter = name;
            if (!string.IsNullOrEmpty(name))
                product = product.Where(i => i.ProductName.Contains(name));
            if (!String.IsNullOrEmpty(category))
            {
                int lop = Convert.ToInt32(category);
                product = product.Where(i => i.CategoryID == lop);
            }
            if (!String.IsNullOrEmpty(pricetext1))
            {
                int price1 = int.Parse(pricetext1);
                int price2 = int.Parse(pricetext2);
                product = product.Where(i => i.Price >= price1 && i.Price<=price2);
            }
            product = product.OrderBy(i => i.ProductName);
            if(product.Count()>0)
            { 
            foreach (var item in product)
            {
                int i = item.Image.IndexOf("|");
                item.Image = item.Image.Substring(0, i);
            }
            }
            int pageSize = 3;
            int pageNumber = (page ?? 1);
            return View(product.ToPagedList(pageNumber, pageSize));
        }

        [HttpPost]
        public ActionResult Login(string username, string pass)
        {
            if (ModelState.IsValid)
            {
                var dao = new AccountDAO();
                int result = dao.Login(username, Encryptor.MD5Hash(pass));
                if (result == 1)
                {
                    var user = dao.GetUser(username);
                    if (user.Status == "true")
                    {
                        Session["User"] = user;

                        DaysLeft daysleft = dao.CountDayLeft(user.AccountID);
                        Session["DaysLeft"] = daysleft;
                        var cart = ShoppingCart.GetCart(this.HttpContext, null);
                        cart.MigrateCart(username);
                        Session.Remove("CartID");
                        if (cart.GetCount()>0)
                            return RedirectToAction("Index","CartItem");
                        return RedirectToAction("Index");
                    }
                    else
                        ViewBag.Message="Bạn không thể đăng nhập bằng tài khoản này.";
                }
                else
                    ViewBag.Message="Tài khoản hoặc mật khẩu không đúng. Vui lòng đăng nhập lại!";
            }
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (Session["User"] != null)
                return RedirectToAction("Index");
            ViewBag.Message = TempData["Message"];
            return View();
        }

        public ActionResult Logout()
        {
            Session.Remove("User");
            Session.Remove("DaysLeft");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Message = TempData["Message"];
            return View();
        }

        [HttpPost]
        public ActionResult Register(Account model)
        {
            if (ModelState.IsValid)
            {
                var dao = new AccountDAO();
                if (dao.CheckUserName(model.UserName))
                {
                    ViewBag.Message = "Tên đăng nhập đã tồn tại";
                }
                //else if (dao.CheckEmail(model.Email))
                //{
                //    ModelState.AddModelError("", "Email đã tồn tại");
                //}
                else
                {
                    var user = new Account();
                    user.UserName = model.UserName;                    
                    user.Pass = Encryptor.MD5Hash(model.Pass);
                    user.Phone = model.Phone;                    
                    user.Address = model.Address;
                    user.Email = model.Email;
                    user.Level = 2;
                    user.CreatedDate = DateTime.Now;
                    user.ExpiryDate = DateTime.Now;
                    user.Status = "false";
                    var result = dao.Insert(user);
                    if (result > 0)
                    {
                        int userid = dao.GetIdUser(model.UserName);
                        string smtpUserName = "testtmdt123@gmail.com";
                        string smtpPassword = "conheo123";
                        string smtpHost = "smtp.gmail.com";
                        int smtpPort = 25;

                        string emailTo = model.Email; 
                        string subject = "Xác minh tài khoản của bạn";
                        string body = string.Format(
                            "<img src='images / feature - pic1.jpg' alt='Banner' /><br/>Xin chào {0}.<br/>Cảm ơn bạn đã đăng kí tài khoản tại TATSHOP. Vui lòng nhấn vào đường dẫn bên dưới để hoàn tất đăng kí.<br/><a href=\"{1}\" title=\"Xác nhận tài khoản của bạn\">Xác nhận</a>", model.UserName, Url.Action("ConfirmEmail", "Home", new { Token = userid, Email = model.Email }, Request.Url.Scheme));

                        EmailService service = new EmailService();

                        bool kq = service.Send(smtpUserName, smtpPassword, smtpHost, smtpPort,
                            emailTo, subject, body);
                        
                        model = new Account();                        
                        return RedirectToAction("Confirm","Home", new { Email = user.Email });
                    }
                    else
                    {
                        ViewBag.Message = "Đăng kí chưa thành công. Vui lòng thử lại.";
                    }
                }
            }
            return View(model);
        }

        public ActionResult ConfirmEmail(string Token, string Email)
        {
            var dao=new AccountDAO();
            Account user = dao.FindById(Int16.Parse(Token));
            if (user != null)
            {
                if (user.Email == Email)
                {
                    dao.UpdateStatusUser(Int16.Parse(Token));
                    TempData["Message"] = "Tài khoản của bạn đã được kích hoạt. Vui lòng đăng nhập tại đây.";
                    return RedirectToAction("Login", "Home");
                }
                else
                {
                    return RedirectToAction("Confirm", "Home", new { Email = user.Email });
                }
            }
            else
            {
                TempData["Message"] = "Tài khoản này không có trong hệ thống. Vui lòng đăng kí tại đây.";
                return RedirectToAction("Register", "Home");
            }
        }
        public ActionResult Confirm(string Email)
        {
            ViewBag.Email = Email;
            return View();
        }
                

        [HttpGet]
        public ActionResult Upgrade()
        {
            return View();
        }

        public ActionResult Result(string tx, string st, string amt, string cc, string cm, string item_number)
        {
                if(Session["User"]!=null)
                {
                    var user = Session["User"] as TMDT.Account;
                    var dao = new OrderDAO();
                    Order order = new Order();
                    order.AccountID = user.AccountID;
                    order.CreatedDate = DateTime.Now;
                    order.Quantity = 1;
                    order.Total = 50;
                    dao.Insert(order);
                    new AccountDAO().UpgradeAccount(user.AccountID);
                    user.Level= 1;
                    Session.Remove("User");
                    Session["User"] = user;
                    return RedirectToAction("Sell", "Merchant");
                }     
                else
                    return RedirectToAction("Login", "Home"); 
        }

        [ChildActionOnly]
        public ActionResult Menu()
        {
            var model = new CategoryDAO().ListAll();
            return PartialView(model);
        }

        public ActionResult Category(int id)
        {
            var model = new ProductDAO().GetProduct(id);
            foreach (var item in model)
            {
                int i = item.Image.IndexOf("|");
                item.Image = item.Image.Substring(0, i);
            }
            return View(model);
        }
		
		public JsonResult SendRating(string r, string s, int id, string url)
        {
            TMDTModel db = new TMDTModel();
            int autoId = id;
            Int16 thisVote = 0;
            Int16 sectionId = 0;
            Int16.TryParse(s, out sectionId);
            Int16.TryParse(r, out thisVote);
            if (autoId.Equals(0))
            {
                return Json("Phần đánh giá không thể thực hiện.");
            }

            switch (s)
            {
                case "5": 
                    Account user = Session["User"] as Account;
                    var isIt = db.VoteModels.Where(m => m.BuyerID == user.AccountID && m.MerchantID == autoId).FirstOrDefault();
                    if (isIt != null)
                    {
                        // keep the school voting flag to stop voting by this member
                        HttpCookie cookie = new HttpCookie(url, "true");
                        Response.Cookies.Add(cookie);
                        return Json("<br />Bạn đã đánh giá người bán này rồi.");
                    }
                    Account sch = db.Accounts.Where(sc => sc.AccountID == autoId).First();
                    if (!sch.Equals(null))
                    {

                        try
                        {
                            sch.Rating = (double)(thisVote + sch.Rating) / (double)(sch.NoRating + 1);
                            sch.NoRating += 1;
                            db.Entry(sch).State = EntityState.Modified;

                            db.SaveChanges();
                            
                            VoteModel vm = new VoteModel()
                            {
                                BuyerID = user.AccountID,
                                MerchantID = autoId,
                                NoVotes = thisVote,
                            };

                            db.VoteModels.Add(vm);
                            db.SaveChanges();
                            HttpCookie cookie = new HttpCookie(url, "true");
                            Response.Cookies.Add(cookie);

                        }
                        catch (DbEntityValidationException e)
                        {
                            foreach (var eve in e.EntityValidationErrors)
                            {
                                Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                    eve.Entry.Entity.GetType().Name, eve.Entry.State);
                                foreach (var ve in eve.ValidationErrors)
                                {
                                    Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                                        ve.PropertyName, ve.ErrorMessage);
                                }
                            }
                            throw;
                        }

                         //keep the school voting flag to stop voting by this member
                        
                    }
                    break;
                default:
                    break;
            }
            return Json("<br />Bạn đã đánh giá " + r + " sao. Cảm ơn bạn.");
        }

        

        

        [HttpGet]
        public ActionResult AccountInfo(int id)
        {
            var user = Session["User"] as TMDT.Account;
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }
            var model = new AccountDAO().GetUser_ID(id);
            return View(model);
        }
    }
}