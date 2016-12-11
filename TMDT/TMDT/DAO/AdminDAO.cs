using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList;
using TMDT.Others;
namespace TMDT.DAO
{
    public class AdminDAO
    {
        TMDTModel db = null;
        public AdminDAO()
        {
            db = new TMDTModel();
        }
        public IEnumerable<Account> ListAllUser(int page, int pageSize)
        {
            var model = db.Accounts.Where(x => x.AccountID != 0).OrderByDescending(x => x.CreatedDate).ToPagedList(page, pageSize);
            return model;
        }
        public IEnumerable<Order> ListAllDonHang(int page, int pageSize)
        {
            var model = db.Orders.OrderBy(s => s.CreatedDate).ToPagedList(page, pageSize);
            return model;
        }
        public IEnumerable<Bill> ListAllHoaDon(int page, int pageSize)
        {
            var model = db.Bills.OrderBy(s => s.CreatedDate).ToPagedList(page, pageSize);
            return model;
        }
        public IEnumerable<DetailBill> ListChiTietHoaDon(int id, int page, int pageSize)
        {
            var model = db.DetailBills.Where(s => s.BillID == id).OrderBy(s => s.AccountID).ToPagedList(page, pageSize);
            return model;
        }
        public IEnumerable<Account> ListRating(int page, int pageSize)
        {
            var model = db.Accounts.OrderByDescending(s => s.Rating).ToPagedList(page, pageSize);
            return model;
        }
        public IEnumerable<Account> ListOptionalRating(int isit, int page, int pageSize)
        {
            var model = db.Accounts.OrderBy(s => s.Rating).ToPagedList(page, pageSize);
            if (isit == 1)
                model = db.Accounts.OrderByDescending(s => s.Rating).Take(5).ToPagedList(page, pageSize);
            else
                model = db.Accounts.OrderBy(s => s.Rating).Take(5).ToPagedList(page, pageSize);
            return model;
        }
        public void LockUser(int id)
        {
            var user = db.Accounts.Find(id);
            if (user.Status == "false")
                user.Status = "true";
            else
                user.Status = "false";
            db.SaveChanges();
        }
        public void ResetPass(int id)
        {
            var user = db.Accounts.Find(id);
            user.Pass = Encryptor.MD5Hash("123456");
            db.SaveChanges();
        }
        public string getemail(int id)
        {
            var user = db.Accounts.Find(id);
            string email = user.Email;
            return email;
        }
        public string getname(int id)
        {
            var user = db.Accounts.Find(id);
            string name = user.UserName;
            return name;
        }
    }
}