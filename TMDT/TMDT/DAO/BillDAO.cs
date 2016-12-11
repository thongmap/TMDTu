using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web;
namespace TMDT.DAO
{
    public class BillDAO
    {
        TMDTModel db = null;
        public BillDAO()
        {
            db = new TMDTModel();
        }

        public void Insert(Bill bill)
        {
            foreach(var item in bill.DetailBills)
            {
                var product = db.Products.Where(x => x.ProductID == item.ProductID);
                foreach(var pro in product)
                {
                    item.Product.ProductName = pro.ProductName;
                    item.Product.Info = pro.Info;
                    item.Product.Quantity = pro.Quantity;
                    item.Product.Price = pro.Price;
                    item.Product.Image = pro.Image;
                }
                
            }
            
            db.Bills.Add(bill);
            db.SaveChanges();        
        }        

        public List<Bill> ViewOrder(int? index, int? limitTime, int? status,int userid)
        {
            List<Bill> listbill=new List<Bill>();
            var bill = db.Bills.Where(x=>x.AccountID==userid).ToList();
            if(index==0)
            {                
                foreach(var b in bill)
                {
                    if (limitTime == null && status==null)
                    {
                        listbill.Add(b);
                    }
                    if(limitTime==0)
                    {
                        TimeSpan time = DateTime.Now - b.CreatedDate;
                        int timepast = time.Days;
                        if(timepast<=30)
                        {
                            listbill.Add(b);
                        }
                    }
                    if (limitTime == 1)
                    {
                        DateTime time = DateTime.Now.AddMonths(-6);
                        
                        if (b.CreatedDate >= time)
                        {
                            listbill.Add(b);
                        }
                    }
                    if (limitTime == 2)
                    {
                        if (b.CreatedDate.Year == DateTime.Now.Year)
                        {
                            listbill.Add(b);
                        }
                    }
                    if(status==0)
                    {
                        if(b.Status=="Chưa hoàn tất")
                            listbill.Add(b);
                    }
                    if (status == 1)
                    {
                        if(b.Status=="Hoàn tất")
                            listbill.Add(b);
                    }
                }
            }
            if (index == 1)
            {
                var model = (from b in db.Bills
                             join d in db.DetailBills on b.BillID equals d.BillID
                             where d.AccountID == userid
                             select new
                             {
                                 billid = b.BillID,
                                 summoney = b.SumMoney,
                                 createddate = b.CreatedDate,
                                 status = b.Status
                             }).AsEnumerable().Select(x => new Bill()
                             {
                                 BillID = x.billid,
                                 SumMoney = x.summoney,
                                 CreatedDate = x.createddate,
                                 Status = x.status
                             }).ToList();
                foreach (var b in model)
                {
                    if (limitTime == null && status==null)
                    {
                        listbill.Add(b);
                    }
                    if (limitTime == 0)
                    {
                        TimeSpan time = DateTime.Now - b.CreatedDate;
                        int timepast = time.Days;
                        if (timepast <= 30)
                        {
                            listbill.Add(b);
                        }
                    }
                    if (limitTime == 1)
                    {
                        DateTime time = DateTime.Now.AddMonths(-6);

                        if (b.CreatedDate >= time)
                        {
                            listbill.Add(b);
                        }
                    }
                    if (limitTime == 2)
                    {
                        if (b.CreatedDate.Year == DateTime.Now.Year)
                        {
                            listbill.Add(b);
                        }
                    }
                    if (status == 0)
                    {
                        if (b.Status == "Chưa hoàn tất")
                            listbill.Add(b);
                    }
                    if (status == 1)
                    {
                        if (b.Status == "Hoàn tất")
                            listbill.Add(b);
                    }
                }
            }
            if(index==null && limitTime==null)
            {
                listbill = bill;
            }
            return listbill;
        }

        public List<DetailBill> GetDetailBill_BillID(int id)
        {
            var model=db.DetailBills.Where(x => x.BillID == id).OrderBy(x=>x.AccountID).ToList();
            foreach(var m in model)
            {
                m.Product.ProductName = db.Products.Find(m.ProductID).ProductName;
                m.Product.Account.UserName= db.Accounts.Find(m.AccountID).UserName;
            }
            return model;
        }

        public void ChangeStatus(int billid)
        {
            var model = db.Bills.Find(billid);
            model.Status = "Hoàn tất";
            db.SaveChanges();
        }

        public List<Bill> GetAllBill(int customerid)
        {
            return db.Bills.Where(x => x.AccountID == customerid).ToList();
        }

        public List<Bill> Sort(int? sort, int? date, int? sum,int merchantid)
        {
            List<Bill> listbill= new List<Bill>();
            var model = (from b in db.Bills
                         join d in db.DetailBills on b.BillID equals d.BillID
                         where d.AccountID == merchantid
                         select new
                         {
                             billid = b.BillID,
                             summoney = b.SumMoney,
                             createddate = b.CreatedDate,
                             status = b.Status
                         }).AsEnumerable().Select(x => new Bill()
                         {
                             BillID = x.billid,
                             SumMoney = x.summoney,
                             CreatedDate = x.createddate,
                             Status = x.status
                         });
            if(sort==0)
            {
                if(date==0)
                {
                    model = model.OrderByDescending(x => x.CreatedDate);
                    listbill = model.ToList();
                }
                if (date == 1)
                {
                    model = model.OrderBy(x => x.CreatedDate);
                    listbill = model.ToList();
                }
            }
            if (sort == 1)
            {
                if (sum == 0)
                {
                    model = model.OrderByDescending(x => x.SumMoney);
                    listbill = model.ToList();
                }
                if (sum == 1)
                {
                    model = model.OrderBy(x => x.SumMoney);
                    listbill = model.ToList();
                }
            }
            return listbill;
        }
        
    }
}