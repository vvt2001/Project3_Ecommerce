using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using project3_ecommerce.Models;
using Newtonsoft.Json;
using System.Data.Entity.Validation;
using PagedList;

namespace project3_ecommerce.Controllers
{
    public class ProductController : Controller
    {
        private ProductEntities db = new ProductEntities();
        private AccountEntities db2 = new AccountEntities();

        // Trang chủ
        public ActionResult Index(int? page)
        {
            int pageSize = 4;
            int pageNumber = (page ?? 1);
            return View(db.Products.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize));
        }

        // Key lưu chuỗi json của Cart
        public const string CARTKEY = "Cart";

        // Lấy cart từ Session (danh sách CartItem)
        List<CartItem> GetCartItems()
        {
            string jsoncart = (Session[CARTKEY] ?? "").ToString();
            if (jsoncart != "")
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(jsoncart);
            }
            return new List<CartItem>();
        }

        // Xóa cart khỏi session
        void ClearCart()
        {
            Session.Remove(CARTKEY);
        }

        // Lưu Cart (Danh sách CartItem) vào session
        void SaveCartSession(List<CartItem> ls)
        {
            string jsoncart = JsonConvert.SerializeObject(ls);
            Session[CARTKEY] = jsoncart;

            //Lưu cart vào database cho lần đăng nhập sau
            try
            {
                Account currentAccount = db2.Accounts.Find((long)Session["ID"]);
                currentAccount.CartInfo = Session[CARTKEY].ToString();
                db2.Entry(currentAccount).State = EntityState.Modified;
                db2.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    var message = "Entity of type " + eve.Entry.Entity.GetType().Name + " in state " + eve.Entry.State + " has the following validation errors:";
                    foreach (var ve in eve.ValidationErrors)
                    {
                        var message2 = "- Property: " + ve.PropertyName + " Error: " + ve.ErrorMessage;
                    }
                }
                throw;
            }
        }

        //Thêm sản phẩm vào cart
        [HttpPost]
        public ActionResult AddToCart(int ID)
        {
            //Nếu chưa đăng nhập thì chuyển đến trang đăng nhập
            if(Session["ID"] == null)
            {
                return Json(new { Message = "login", JsonRequestBehavior.AllowGet });
            }

            var product = db.Products
                .Where(p => p.ID == ID)
                .FirstOrDefault();
            if (product == null)
                return HttpNotFound("Không có sản phẩm");

            // Xử lý đưa vào Cart ...
            var cart = GetCartItems();
            if (cart != null)
            {
                var cartitem = cart.Find(p => p.Product.ID == ID);
                if (cartitem != null)
                {
                    // Đã tồn tại, tăng thêm 1
                    cartitem.Quantity++;
                }
                else
                {
                    //  Thêm mới
                    cart.Add(new CartItem() { Quantity = 1, Product = product });
                }
            }
            else
            {
                //  Thêm mới
                cart.Add(new CartItem() { Quantity = 1, Product = product });
            }

            // Lưu cart vào Session
            SaveCartSession(cart);

            string message = "SUCCESS";
            return Json(new { Message = message, JsonRequestBehavior.AllowGet });
        }

        //Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public ActionResult UpdateCart(int ID, int Quantity)
        {
            // Cập nhật Cart thay đổi số lượng quantity ...
            var cart = GetCartItems();
            var cartitem = cart.Find(p => p.Product.ID == ID);
            if (cartitem != null)
            {
                cartitem.Quantity = Quantity;
            }
            SaveCartSession(cart);

            // Trả về mã thành công
            string message = "SUCCESS";
            return Json(new { Message = message, JsonRequestBehavior.AllowGet });
        }

        //Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public ActionResult DeleteCartItem(int ID)
        {
            var cart = GetCartItems();
            var cartitem = cart.Find(p => p.Product.ID == ID);
            if (cartitem != null)
            {
                cart.Remove(cartitem);
            }

            SaveCartSession(cart);

            if (cart.Count != 0 && cart != null )
                ViewBag.IsEmpty = false;
            else
                ViewBag.IsEmpty = true;

            string message = "SUCCESS";
            return Json(new { Message = message, JsonRequestBehavior.AllowGet });
        }

        // Hiển thị giỏ hàng
        public ActionResult Cart()
        {
            var cart = GetCartItems();
            int count = 0;
            List<long> CartItemsID = new List<long>();
            if(cart != null && cart.Count != 0)
            {
                foreach (var item in cart)
                {
                    CartItemsID.Add(item.Product.ID);
                    count++;
                }
                ViewBag.CartItemsID = CartItemsID;
                ViewBag.IDCount = count;
            }

            return View(cart);
        }

        public ActionResult CartCheck()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        public ActionResult ConfirmPurchase()
        {
            Session[CARTKEY] = null;
            Account currentAccount = db2.Accounts.Find((long)Session["ID"]);
            currentAccount.CartInfo = null;
            db2.Entry(currentAccount).State = EntityState.Modified;
            db2.SaveChanges();

            return View();
        }

        public ActionResult Create()
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                                     select new
                                                     {
                                                         ID = (int)e,
                                                         Name = e.ToDescriptionString()
                                                     };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");
            return View();
        }

        [HttpPost]
        public ActionResult Create(Product product)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            product.Category = product.Category_EnumValue.ToDescriptionString();

            db.Products.Add(product);
            db.SaveChanges();
            return RedirectToAction("GetList");
        }

        public ActionResult GetList(int? page)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(db.Products.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize));
        }
        [HttpPost]
        public ActionResult GetList(SearchViewModel SearchViewModel, int? page)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(db.Products.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize));
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
