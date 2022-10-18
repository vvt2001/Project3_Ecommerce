using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using project3_ecommerce.Models;

namespace project3_ecommerce.Controllers
{
    public class AccountController : Controller
    {
        private AccountEntities db = new AccountEntities();

        // GET: Login/Register
        public ActionResult Register()
        {
            var account = new Account();
            return View(account);
        }

        private static byte[] GetKey()
        {
            var key = new byte[32];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(key);
            }

            return key;
        }

        private static byte[] ConnectByte(byte[] byteArr1, byte[] byteArr2)
        {
            byte[] result = new byte[byteArr1.Length + byteArr2.Length];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = i < byteArr1.Length ? byteArr1[i] : byteArr2[i - byteArr1.Length];
            }
            return result;
        }

        private static byte[] MD5Hashing(byte[] byteArr)
        {
            byte[] hashBytes;
            // Creates an instance of the default implementation of the MD5 hash algorithm.
            using (var md5Hash = MD5.Create())
            {
                // Generate hash value(byte Array) for input data
                hashBytes = md5Hash.ComputeHash(byteArr);
            }
            return hashBytes;
        }

        // POST: Login/Register
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public ActionResult Register(Account account)
        {
            var accountDetail = db.Accounts.Where(x => x.Username == account.Username).FirstOrDefault();
            if (accountDetail == null)
            {
                if (account.Password == account.confirmPassword)
                {
                    var key = GetKey();
                    var bytePassword = Encoding.ASCII.GetBytes(account.Password);
                    var connectedByte = ConnectByte(bytePassword, key);

                    account.HashedPassword = MD5Hashing(connectedByte);
                    account.Key = key;

                    if (ModelState.IsValid)
                    {
                        db.Accounts.Add(account);
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    account.registerErrorMessage = "Mật khẩu xác nhận không đúng.";
                    return View(account);
                }
            }
            else
            {
                account.registerErrorMessage = "Tên tài khoản đã tồn tại.";
                return View(account);
            }
            return View(account);
        }

        // GET: Login
        public ActionResult Index()
        {
            var account = new Account();
            return View(account);
        }

        // POST: Login
        [HttpPost]
        public ActionResult Authorize(Account account)
        {
            var accountDetail = db.Accounts.Where(x => x.Username == account.Username).FirstOrDefault();
            if (accountDetail == null)
            {
                account.loginErrorMessage = "Sai tên tài khoản hoặc mật khẩu.";
                return View("Index", account);
            }
            else
            {
                var bytePassword = Encoding.ASCII.GetBytes(account.Password);
                var key = accountDetail.Key;

                var connectedByte = ConnectByte(bytePassword, key);
                var hashInputPassword = MD5Hashing(connectedByte);

                if (accountDetail.HashedPassword.SequenceEqual(hashInputPassword))
                {
                    Session["ID"] = accountDetail.ID;
                    Session["Username"] = accountDetail.Username;
                    HttpCookie Usercookie = new HttpCookie("ID", Session["ID"].ToString());
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    account.loginErrorMessage = "Sai tên tài khoản hoặc mật khẩu.";
                    return View("Index", account);
                }
            }
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Index", "Accounts");
        }

        public ActionResult ChangePassword(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account CurrentAccount = db.Accounts.Find(id);
            if (CurrentAccount == null)
            {
                return HttpNotFound();
            }
            return View(CurrentAccount);
        }

        [HttpPost]
        public ActionResult ChangePassword(Account account)
        {
            var NewAccountDetail = account;
            if (NewAccountDetail != null)
            {
                if (account.Password == account.confirmPassword)
                {
                    var key = account.Key;
                    var bytePassword = Encoding.ASCII.GetBytes(account.Password);
                    var connectedByte = ConnectByte(bytePassword, key);

                    NewAccountDetail.HashedPassword = MD5Hashing(connectedByte);

                    if (ModelState.IsValid)
                    {
                        db.Entry(NewAccountDetail).State = EntityState.Modified;

                        try
                        {
                            db.SaveChanges();
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


                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        account.registerErrorMessage = "Có lỗi trong thay đổi mật khẩu.";
                        return View(account);
                    }
                }
                else
                {
                    account.registerErrorMessage = "Mật khẩu xác nhận không đúng.";
                    return View(account);
                }
            }
            else
            {
                return HttpNotFound();
            }
        }
    }
}
