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
using System.IO;

namespace project3_ecommerce.Controllers
{
    public class ProductController : Controller
    {
        private ProductEntities db = new ProductEntities();
        private AccountEntities db2 = new AccountEntities();
        private SalesEntities db3 = new SalesEntities();

        // Trang chủ
        public ActionResult Index(int? page)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            SearchViewModel SearchViewModel = new SearchViewModel();

            int pageSize = 8;
            int pageNumber = (page ?? 1);

            SearchViewModel.ProductModel = db.Products.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize);
            return View(SearchViewModel);
        }

        [HttpPost]
        public ActionResult Index(SearchViewModel SearchViewModel, int? page)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            var searchModel = SearchViewModel.SearchModel;
            var result = db.Products.AsEnumerable();

            if (searchModel.Name != null)
                result = result.Where(x => x.Name.Contains(searchModel.Name));
            if (searchModel.Category != null)
                result = result.Where(x => x.Category.Contains(searchModel.Category.ToDescriptionString()));
            if (searchModel.LowerPrice != null)
                result = result.Where(x => x.Price >= searchModel.LowerPrice);
            if (searchModel.UpperPrice != null)
                result = result.Where(x => x.Price <= searchModel.UpperPrice);

            if (result.Any())
            {
                int pageSize = 10;
                int pageNumber = (page ?? 1);
                SearchViewModel.ProductModel = result.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize);
            }

            return View("Index", SearchViewModel);
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
        public ActionResult AddToCart(int ID, int? quantity)
        {
            int realQuantity = 1;
            if(quantity != null)
            {
                realQuantity = (int)quantity;
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
                    cartitem.Quantity += realQuantity;
                }
                else
                {
                    //  Thêm mới
                    cart.Add(new CartItem() { Quantity = realQuantity, Product = product });
                }
            }
            else
            {
                //  Thêm mới
                cart.Add(new CartItem() { Quantity = realQuantity, Product = product });
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
            DateTime currentDay = DateTime.Today;
            DateTime currentMonth = new DateTime(currentDay.Year, currentDay.Month, 1);

            Sale thisMonthSales = new Sale()
            {
                Tháng = new DateTime(currentDay.Year, currentDay.Month, 1),
                Doanh_thu_Máy_tính = 0,
                Doanh_thu_Điện_thoại = 0,
                Doanh_thu_Phụ_kiện = 0,
                Doanh_thu_Khác = 0,
                Số_sản_phẩm_bán_được_Máy_tính = 0,
                Số_sản_phẩm_bán_được_Điện_thoại = 0,
                Số_sản_phẩm_bán_được_Phụ_kiện = 0,
                Số_sản_phẩm_bán_được_Khác = 0,
                Số_sản_phẩm_còn_lại_Máy_tính = db.Products.Where(x => x.Category == "Máy tính").Sum(item => item.RemainStocks),
                Số_sản_phẩm_còn_lại_Điện_thoại = db.Products.Where(x => x.Category == "Điện thoại").Sum(item => item.RemainStocks),
                Số_sản_phẩm_còn_lại_Phụ_kiện = db.Products.Where(x => x.Category == "Phụ kiện").Sum(item => item.RemainStocks),
                Số_sản_phẩm_còn_lại_Khác = db.Products.Where(x => x.Category == "Khác").Sum(item => item.RemainStocks),
            };

            if (db3.Sales.Where(x => x.Tháng == currentMonth).Count() == 0)
            {
                db3.Sales.Add(thisMonthSales);
                db3.SaveChanges();
            }
            else
            {
                thisMonthSales = db3.Sales.Where(x => x.Tháng == currentMonth).First();
            }

            var cart = GetCartItems();
            foreach(var item in cart)
            {
                Product currentProduct = db.Products.Find(item.Product.ID);

                if(currentProduct.RemainStocks < item.Quantity)
                {
                    ViewBag.OutOfStocks = "Số lượng hàng trong kho không đủ.";
                    return View("CartCheck",cart);
                }

                switch (item.Product.Category){
                    case "Máy tính":
                        thisMonthSales.Doanh_thu_Máy_tính += item.Product.Price * item.Quantity;
                        thisMonthSales.Số_sản_phẩm_bán_được_Máy_tính += item.Quantity;
                        currentProduct.RemainStocks -= item.Quantity;
                        thisMonthSales.Số_sản_phẩm_còn_lại_Máy_tính -= item.Quantity;
                        break;
                    case "Điện thoại":
                        thisMonthSales.Doanh_thu_Điện_thoại += item.Product.Price * item.Quantity;
                        thisMonthSales.Số_sản_phẩm_bán_được_Điện_thoại += item.Quantity;
                        currentProduct.RemainStocks -= item.Quantity;
                        thisMonthSales.Số_sản_phẩm_còn_lại_Điện_thoại -= item.Quantity;
                        break;
                    case "Phụ kiện":
                        thisMonthSales.Doanh_thu_Phụ_kiện += item.Product.Price * item.Quantity;
                        thisMonthSales.Số_sản_phẩm_bán_được_Phụ_kiện += item.Quantity;
                        currentProduct.RemainStocks -= item.Quantity;
                        thisMonthSales.Số_sản_phẩm_còn_lại_Phụ_kiện -= item.Quantity;
                        break;
                    case "Khác":
                        thisMonthSales.Doanh_thu_Khác += item.Product.Price * item.Quantity;
                        thisMonthSales.Số_sản_phẩm_bán_được_Khác += item.Quantity;
                        currentProduct.RemainStocks -= item.Quantity;
                        thisMonthSales.Số_sản_phẩm_còn_lại_Khác -= item.Quantity;
                        break;
                    default:
                        ViewBag.OutOfStocks = "Có lỗi trong xác nhận thanh toán.";
                        return View("CartCheck", cart);
                }

                db.Entry(currentProduct).State = EntityState.Modified;
                db.SaveChanges();
            }

            db3.Entry(thisMonthSales).State = EntityState.Modified;
            db3.SaveChanges();

            Session[CARTKEY] = null;
            Account currentAccount = db2.Accounts.Find((long)Session["ID"]);
            currentAccount.CartInfo = null;
            db2.Entry(currentAccount).State = EntityState.Modified;
            db2.SaveChanges();

            return View();
        }
        private static string GetPath(string path, int count)
        {
            if (System.IO.File.Exists(path))
            {
                count += 1;
                if (count != 2)
                {
                    int duplicateIndicatorCharCount = 3 + count.ToString().Length;
                    path = path.Remove(path.LastIndexOf('.') - duplicateIndicatorCharCount, duplicateIndicatorCharCount);
                }

                string duplicateIndicator = " (" + count + ")";
                path = path.Insert(path.LastIndexOf('.'), duplicateIndicator);
                return GetPath(path, count);
            }
            else
            {
                return path;
            }

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

            if (product.ImageFileUpload != null)
            {
                Stream fileStream = product.ImageFileUpload.InputStream;
                BinaryReader binaryReader = new BinaryReader(fileStream);
                byte[] FileDetail = binaryReader.ReadBytes((Int32)fileStream.Length);

                product.Image = FileDetail;

                //Save file to designated dir
                System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploaded Files/"));
                string path = Server.MapPath("~/Uploaded Files/") + product.ImageFileUpload.FileName;

                //Make sure duplicated files is different when stored in server folder
                path = GetPath(path, 1);
                product.ImagePath = "/Uploaded Files/" + Path.GetFileName(path);
                product.ImageFileUpload.SaveAs(path);

            }

            db.Products.Add(product);
            db.SaveChanges();
            return RedirectToAction("Index");
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

            SearchViewModel SearchViewModel = new SearchViewModel();
            SearchViewModel.ProductModel = db.Products.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize);

            return View(SearchViewModel);
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


            var searchModel = SearchViewModel.SearchModel;
            var result = db.Products.AsEnumerable();

            if (searchModel.Name != null)
                result = result.Where(x => x.Name.Contains(searchModel.Name));
            if (searchModel.Category != null)
                result = result.Where(x => x.Category.Contains(searchModel.Category.ToDescriptionString()));
            if (searchModel.LowerPrice != null)
                result = result.Where(x => x.Price >= searchModel.LowerPrice);
            if (searchModel.UpperPrice != null)
                result = result.Where(x => x.Price <= searchModel.UpperPrice);

            if (result.Any())
            {
                int pageSize = 10;
                int pageNumber = (page ?? 1);
                SearchViewModel.ProductModel = result.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize);
            }

            return View("GetList", SearchViewModel);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {

            var product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            string message = "SUCCESS";
            return Json(new { Message = message, JsonRequestBehavior.AllowGet });

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
