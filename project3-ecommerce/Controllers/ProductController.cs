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
        private ImageEntities db1 = new ImageEntities();
        private AccountEntities db2 = new AccountEntities();
        private SalesEntities db3 = new SalesEntities();
        private InfoEntities db4 = new InfoEntities();
        private MessageEntities db5 = new MessageEntities();

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
            SearchViewModel.ImageModel = db1.Images.AsEnumerable();
            ViewBag.ProductSum = db.Products.Count();
            return View(SearchViewModel);
        }

        public ActionResult SearchResult(string Name, Category? Category, long? LowerPrice, long? UpperPrice, int? page)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            var searchModel = new SearchModel() {
                Name = Name,
                Category = Category,
                LowerPrice = LowerPrice,
                UpperPrice = UpperPrice,
            };

            var result = db.Products.AsEnumerable();

            if (searchModel.Name != null)
                result = result.Where(x => x.Name.Contains(searchModel.Name));
            if (searchModel.Category != null && searchModel.Category != Models.Category.Danh_mục_sản_phẩm)
                result = result.Where(x => x.Category.Contains(searchModel.Category.ToDescriptionString()));
            if (searchModel.LowerPrice != null)
                result = result.Where(x => x.Price >= searchModel.LowerPrice);
            if (searchModel.UpperPrice != null)
                result = result.Where(x => x.Price <= searchModel.UpperPrice);

            var SearchViewModel = new SearchViewModel();

            ViewBag.ProductSum = result.Count();

            if (result.Any())
            {

                int pageSize = 8;
                int pageNumber = (page ?? 1);
                SearchViewModel.ProductModel = result.OrderBy(x => x.ID).ToPagedList(pageNumber, pageSize);
                SearchViewModel.SearchModel = searchModel;
            }

            SearchViewModel.ImageModel = db1.Images.AsEnumerable();
            ViewBag.Name = Name;
            if (Category != null && Category != Models.Category.Danh_mục_sản_phẩm)
                ViewBag.Category = Category;
            ViewBag.LowerPrice = LowerPrice;
            ViewBag.UpperPrice = UpperPrice;

            TempData["Name"] = Name;
            if(Category != null && Category != Models.Category.Danh_mục_sản_phẩm)
                TempData["Category"] = Category.ToDescriptionString();
            TempData["LowerPrice"] = LowerPrice;
            TempData["UpperPrice"] = UpperPrice;

            return View("SearchResult", SearchViewModel);
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
        public ActionResult Create(ProductViewModel ProductViewModel)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            ProductViewModel.Product.Category = ProductViewModel.Product.Category_EnumValue.ToDescriptionString();

            //save 1st image as product avatar
            if (ProductViewModel.EmpFileModel.fileUpload != null)
            {
                var fileUpload = ProductViewModel.EmpFileModel.fileUpload.FirstOrDefault();
                if(fileUpload != null)
                {
                    //Save file to designated dir
                    System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploaded Files/"));
                    string path = Server.MapPath("~/Uploaded Files/") + fileUpload.FileName;

                    //Make sure duplicated files is different when stored in server folder
                    path = GetPath(path, 1);
                    ProductViewModel.Product.ImagePath = "/Uploaded Files/" + Path.GetFileName(path);
                }
            }

            db.Products.Add(ProductViewModel.Product);
            db.SaveChanges();

            if (ProductViewModel.EmpFileModel.fileUpload != null)
            {
                foreach (HttpPostedFileBase fileUpload in ProductViewModel.EmpFileModel.fileUpload)
                {
                    if (fileUpload == null)
                    {
                        break;
                    }
                    else if (fileUpload.ContentLength > 20000000)
                    {
                        continue;
                    }
                    else
                    {
                        Image image = new Image();

                        Stream fileStream = fileUpload.InputStream;
                        BinaryReader binaryReader = new BinaryReader(fileStream);
                        byte[] FileDetail = binaryReader.ReadBytes((Int32)fileStream.Length);

                        image.Content = FileDetail;
                        image.FileName = fileUpload.FileName;

                        //Save file to designated dir
                        System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploaded Files/"));
                        string path = Server.MapPath("~/Uploaded Files/") + fileUpload.FileName;

                        //Make sure duplicated files is different when stored in server folder
                        path = GetPath(path, 1);
                        image.ImagePath = "/Uploaded Files/" + Path.GetFileName(path);
                        image.ProductID = ProductViewModel.Product.ID;

                        var fileList = db1.Images.Where(x => x.ProductID == image.ProductID).ToList();
                        if (fileList.FindIndex(x => x.FileName == image.FileName) < 0)
                        {
                            fileUpload.SaveAs(path);
                            db1.Images.Add(image);
                            db1.SaveChanges();
                        }
                        else
                        {
                            ViewBag.FileStatus = "Không được đăng tải file bị trùng.";
                        }

                        fileUpload.SaveAs(path);
                    }
                }
            }


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

            Product Product = db.Products.Find(id);
            IEnumerable<Image> images = db1.Images.Where(x => x.ProductID == Product.ID);
            IEnumerable<Info> info = db4.Infoes.Where(x => x.ProductID == Product.ID);
            ProductViewModel ProductViewModel = new ProductViewModel
            {
                Product = Product,
                Images = images,
                Info = info,
            };
            if (Product == null)
            {
                return HttpNotFound();
            }
            return View(ProductViewModel);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {

            var product = db.Products.Find(id);
            var images = db1.Images.Where(x => x.ProductID == product.ID);
            foreach(var item in images)
            {
                db1.Images.Remove(item);
                if (System.IO.File.Exists(item.ImagePath))
                {
                    System.IO.File.Delete(item.ImagePath);
                }
            }
            db1.SaveChanges();
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

        // GET: Home
        public ActionResult Charts(DateTime? currentTime)
        {
            if (currentTime == null)
            {
                DateTime currentDay = DateTime.Today;
                currentTime = new DateTime(currentDay.Year, currentDay.Month, 1);
            }

            DateTime time = (DateTime)currentTime;

/*            if (currentTime == null)
            {
                currentTime = DateTime.Today.Month;
            }*/
            Sale currentSales = db3.Sales.Where(x => x.Tháng.Month == time.Month).FirstOrDefault();

            List<BarChartModel> barChartDoanhThu = new List<BarChartModel>();
            barChartDoanhThu.Add(new BarChartModel("Máy tính", currentSales.Doanh_thu_Máy_tính));
            barChartDoanhThu.Add(new BarChartModel("Điện thoại", currentSales.Doanh_thu_Điện_thoại));
            barChartDoanhThu.Add(new BarChartModel("Phụ kiện", currentSales.Doanh_thu_Phụ_kiện));
            barChartDoanhThu.Add(new BarChartModel("Khác", currentSales.Doanh_thu_Khác));
            ViewBag.barChartDoanhThu = JsonConvert.SerializeObject(barChartDoanhThu);

            List<BarChartModel> barChartSoSanPhamDaBan = new List<BarChartModel>();
            barChartSoSanPhamDaBan.Add(new BarChartModel("Máy tính", currentSales.Số_sản_phẩm_bán_được_Máy_tính));
            barChartSoSanPhamDaBan.Add(new BarChartModel("Điện thoại", currentSales.Số_sản_phẩm_bán_được_Điện_thoại));
            barChartSoSanPhamDaBan.Add(new BarChartModel("Phụ kiện", currentSales.Số_sản_phẩm_bán_được_Phụ_kiện));
            barChartSoSanPhamDaBan.Add(new BarChartModel("Khác", currentSales.Số_sản_phẩm_bán_được_Khác));
            ViewBag.barChartSoSanPhamDaBan = JsonConvert.SerializeObject(barChartSoSanPhamDaBan);

            List<BarChartModel> barChartSoSanPhamConLai = new List<BarChartModel>();
            barChartSoSanPhamConLai.Add(new BarChartModel("Máy tính", currentSales.Số_sản_phẩm_còn_lại_Máy_tính));
            barChartSoSanPhamConLai.Add(new BarChartModel("Điện thoại", currentSales.Số_sản_phẩm_còn_lại_Điện_thoại));
            barChartSoSanPhamConLai.Add(new BarChartModel("Phụ kiện", currentSales.Số_sản_phẩm_còn_lại_Phụ_kiện));
            barChartSoSanPhamConLai.Add(new BarChartModel("Khác", currentSales.Số_sản_phẩm_còn_lại_Khác));
            ViewBag.barChartSoSanPhamConLai = JsonConvert.SerializeObject(barChartSoSanPhamConLai);

            ViewBag.SelectedTime = time.ToString("MM-yyyy");
            return View(db3.Sales.AsEnumerable());
        }

        public ActionResult LineCharts(int? year)
        {
            if(year == null)
            {
                year = DateTime.Now.Year;
            }
            IEnumerable<Sale> Sales = db3.Sales.Where(x => x.Tháng.Year == year).OrderBy(x=>x.Tháng.Month).AsEnumerable();

            //Bieu do Doanh Thu
            List<LineChartModel> LineChartDoanhThu1 = new List<LineChartModel>();
            List<LineChartModel> LineChartDoanhThu2 = new List<LineChartModel>();
            List<LineChartModel> LineChartDoanhThu3 = new List<LineChartModel>();
            List<LineChartModel> LineChartDoanhThu4 = new List<LineChartModel>();

            foreach(var sale in Sales)
            {
                LineChartDoanhThu1.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Doanh_thu_Máy_tính));
                LineChartDoanhThu2.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Doanh_thu_Điện_thoại));
                LineChartDoanhThu3.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Doanh_thu_Phụ_kiện));
                LineChartDoanhThu4.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Doanh_thu_Khác));
            }

            ViewBag.LineChartDoanhThu1 = JsonConvert.SerializeObject(LineChartDoanhThu1);
            ViewBag.LineChartDoanhThu2 = JsonConvert.SerializeObject(LineChartDoanhThu2);
            ViewBag.LineChartDoanhThu3 = JsonConvert.SerializeObject(LineChartDoanhThu3);
            ViewBag.LineChartDoanhThu4 = JsonConvert.SerializeObject(LineChartDoanhThu4);


            //Bieu do So san pham da ban
            List<LineChartModel> LineChartSoSanPhamDaBan1 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamDaBan2 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamDaBan3 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamDaBan4 = new List<LineChartModel>();

            foreach (var sale in Sales)
            {
                LineChartSoSanPhamDaBan1.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_bán_được_Máy_tính));
                LineChartSoSanPhamDaBan2.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_bán_được_Điện_thoại));
                LineChartSoSanPhamDaBan3.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_bán_được_Phụ_kiện));
                LineChartSoSanPhamDaBan4.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_bán_được_Khác));
            }

            ViewBag.LineChartSoSanPhamDaBan1 = JsonConvert.SerializeObject(LineChartSoSanPhamDaBan1);
            ViewBag.LineChartSoSanPhamDaBan2 = JsonConvert.SerializeObject(LineChartSoSanPhamDaBan2);
            ViewBag.LineChartSoSanPhamDaBan3 = JsonConvert.SerializeObject(LineChartSoSanPhamDaBan3);
            ViewBag.LineChartSoSanPhamDaBan4 = JsonConvert.SerializeObject(LineChartSoSanPhamDaBan4);


            //Bieu do So san pham con lai
            List<LineChartModel> LineChartSoSanPhamConLai1 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamConLai2 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamConLai3 = new List<LineChartModel>();
            List<LineChartModel> LineChartSoSanPhamConLai4 = new List<LineChartModel>();

            foreach(var sale in Sales)
            {
                LineChartSoSanPhamConLai1.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_còn_lại_Máy_tính));
                LineChartSoSanPhamConLai2.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_còn_lại_Điện_thoại));
                LineChartSoSanPhamConLai3.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_còn_lại_Phụ_kiện));
                LineChartSoSanPhamConLai4.Add(new LineChartModel(sale.Tháng.Month.ToString(), sale.Số_sản_phẩm_còn_lại_Khác));
            }

            ViewBag.LineChartSoSanPhamConLai1 = JsonConvert.SerializeObject(LineChartSoSanPhamConLai1);
            ViewBag.LineChartSoSanPhamConLai2 = JsonConvert.SerializeObject(LineChartSoSanPhamConLai2);
            ViewBag.LineChartSoSanPhamConLai3 = JsonConvert.SerializeObject(LineChartSoSanPhamConLai3);
            ViewBag.LineChartSoSanPhamConLai4 = JsonConvert.SerializeObject(LineChartSoSanPhamConLai4);

            ViewBag.SelectedTime = year.ToString();
            ViewBag.CurrentYear = DateTime.Now.Year;
            return View(Sales);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product Product = db.Products.Find(id);
            IEnumerable<Image> images = db1.Images.Where(x => x.ProductID == Product.ID);
            ProductViewModel ProductViewModel = new ProductViewModel
            {
                Product = Product,
                Images = images
            };
            if (Product == null)
            {
                return HttpNotFound();
            }
            Product.Category_EnumValue = EnumExtension.GetValueFromDescription<Category>(Product.Category);
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            ViewBag.ProductID = ProductViewModel.Product.ID;
            int count = 0;
            List<long> FilesID = new List<long>();
            foreach (var item in ProductViewModel.Images)
            {
                FilesID.Add(item.ID);
                count++;
            }
            ViewBag.FilesID = FilesID;
            ViewBag.IDCount = count;

            return View(ProductViewModel);
        }

        [HttpPost]
        public ActionResult Edit(ProductViewModel ProductViewModel)
        {
            var Category_EnumData = from Category e in Enum.GetValues(typeof(Category))
                                    select new
                                    {
                                        ID = (int)e,
                                        Name = e.ToDescriptionString()
                                    };
            ViewBag.Category_EnumList = new SelectList(Category_EnumData, "ID", "Name");

            ProductViewModel.Product.Category = ProductViewModel.Product.Category_EnumValue.ToDescriptionString();

            if (ProductViewModel.EmpFileModel.fileUpload != null)
            {
                foreach (HttpPostedFileBase fileUpload in ProductViewModel.EmpFileModel.fileUpload)
                {
                    if (fileUpload == null)
                    {
                        break;
                    }
                    else if (fileUpload.ContentLength > 20000000)
                    {
                        continue;
                    }
                    else
                    {
                        Image image = new Image();

                        Stream fileStream = fileUpload.InputStream;
                        BinaryReader binaryReader = new BinaryReader(fileStream);
                        byte[] FileDetail = binaryReader.ReadBytes((Int32)fileStream.Length);

                        image.FileName = fileUpload.FileName;
                        image.Content = FileDetail;

                        //Save file to designated dir
                        System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploaded Files/"));
                        string path = Server.MapPath("~/Uploaded Files/") + fileUpload.FileName;

                        //Make sure duplicated files is different when stored in server folder
                        path = GetPath(path, 1);
                        image.ImagePath = "/Uploaded Files/" + Path.GetFileName(path);
                        image.ProductID = ProductViewModel.Product.ID;

                        var fileList = db1.Images.Where(x => x.ProductID == image.ProductID).ToList();
                        if (fileList.FindIndex(x => x.FileName == image.FileName) < 0)
                        {
                            fileUpload.SaveAs(path);
                            db1.Images.Add(image);
                            db1.SaveChanges();
                        }
                        else
                        {
                            ViewBag.FileStatus = "Không được đăng tải file bị trùng.";
                        }

                        fileUpload.SaveAs(path);
                    }
                }
            }

            db.Entry(ProductViewModel.Product).State = EntityState.Modified;
            db.SaveChanges();

            ProductViewModel.Images = db1.Images.ToList().Where(x => x.ProductID == ProductViewModel.Product.ID);
            ViewBag.ProductID = ProductViewModel.Product.ID;
            int count = 0;
            List<long> FilesID = new List<long>();
            foreach (var item in ProductViewModel.Images)
            {
                FilesID.Add(item.ID);
                count++;
            }
            ViewBag.FilesID = FilesID;
            ViewBag.IDCount = count;

            return RedirectToAction("GetList");
        }

        [HttpPost]
        public ActionResult UploadFile(long ProductID)
        {

            HttpPostedFileBase[] fileUpload = new HttpPostedFileBase[HttpContext.Request.Files.Count];
            List<long> FilesID = new List<long>();

            for (int i = 0; i < HttpContext.Request.Files.Count; ++i)
            {
                fileUpload[i] = HttpContext.Request.Files[i];
            }
            if (fileUpload != null)
            {
                 foreach (HttpPostedFileBase item in fileUpload)
                {
                    if (item == null)
                    {
                        break;
                    }
                    else if (item.ContentLength > 20000000)
                    {
                        continue;
                    }
                    else
                    {
                        Image image = new Image();

                        Stream fileStream = item.InputStream;
                        BinaryReader binaryReader = new BinaryReader(fileStream);
                        byte[] FileDetail = binaryReader.ReadBytes((Int32)fileStream.Length);

                        image.FileName = item.FileName;
                        image.Content = FileDetail;

                        //Save file to designated dir
                        System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploaded Files/"));
                        string path = Server.MapPath("~/Uploaded Files/") + item.FileName;

                        //Make sure duplicated files is different when stored in server folder
                        path = GetPath(path, 1);
                        image.ImagePath = "/Uploaded Files/" + Path.GetFileName(path);
                        image.ProductID = ProductID;

                        var fileList = db1.Images.Where(x => x.ProductID == image.ProductID).ToList();
                        if (fileList.FindIndex(x => x.FileName == image.FileName) < 0)
                        {
                            item.SaveAs(path);
                            db1.Images.Add(image);
                            db1.SaveChanges();
                        }
                        else
                        {
                            ViewBag.FileStatus = "Không được đăng tải file bị trùng.";
                        }

                        item.SaveAs(path);
                    }
                }
            }

            return Json(FilesID, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult DeleteFileAjax(int FileID)
        {
            Image file = db1.Images.Find(FileID);
            if (System.IO.File.Exists(file.ImagePath))
            {
                System.IO.File.Delete(file.ImagePath);
            }
            db1.Images.Remove(file);
            db1.SaveChanges();

            string message = "SUCCESS";
            return Json(new { Message = message, JsonRequestBehavior.AllowGet });
        }
        [HttpGet]
        public FileResult DownLoadFile(int id)
        {
            List<Image> ObjFiles = db1.Images.ToList();

            var FileById = (from FC in ObjFiles
                            where FC.ID.Equals(id)
                            select new { FC.FileName, FC.Content }).ToList().FirstOrDefault();

            return File(FileById.Content, "application/pdf", FileById.FileName);
        }

        public PartialViewResult Notifications()
        {
            var Messages = db5.Messages.AsEnumerable();
            return PartialView(Messages);
        }
    }
}
