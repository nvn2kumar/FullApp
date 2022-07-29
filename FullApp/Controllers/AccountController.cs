using FullApp.Models;
using FullApp.Models.ViewModel;
using FullApp.Repository.Contract;
using FullApp.Utils.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FullApp.Controllers
{
    public class AccountController : Controller
    {
        private IUser userService;
        private CustomerContext _context;
        private readonly IConfiguration _config;
        private readonly IHostEnvironment hostingEnvironment;

        public AccountController(IUser user, IHostEnvironment hostingEnvironment, CustomerContext dbcontext, IConfiguration config)
        {
            userService = user;
            this.hostingEnvironment = hostingEnvironment;
            _context = dbcontext;
            _config = config;
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(SignIn model)
        {
            if (ModelState.IsValid)
            {
                var result = userService.AuthenticateUser(model);
                if (result == AuthoEnum.SUCCESS)
                {
                    //Create the identity for the user  
                    var identity = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, model.Email)
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                    var principal = new ClaimsPrincipal(identity);

                    var login = HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


                    return RedirectToAction("Index", "Home");
                }
                else if (result == AuthoEnum.FAILED)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login credentails !");
                    return View();
                }
                else if (result == AuthoEnum.NOTVERIFIED)
                {
                    ModelState.AddModelError(string.Empty, "Your account is still not active, please verify your account !");
                    return View();
                }
                ModelState.AddModelError(string.Empty, "You are not a valid user !");
                return View();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please enter login details !");
                return View();
            }
        }

        public IActionResult Logout()
        {
            var login = HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        public IActionResult UpdateProfile()
        {
            return View();
        }

        public string UploadProfile(IFormFile file)
        {
            var www = hostingEnvironment.ContentRootPath;
            var fullpath = Path.Combine(www, "wwwroot", "images", file.FileName);

            FileStream stream = new FileStream(fullpath, FileMode.Create);
            file.CopyTo(stream);
            return $"images/{file.FileName}";
        }
        [HttpPost]
        public IActionResult UpdateProfile(string Email)
        {
            var file = Request.Form.Files;
            if (file.Count > 0)
            {
                string path = UploadProfile(file[0]);
                userService.UpdateProfile(Email, path);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please select file !");
            }
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(SignUp model)
        {
            if (ModelState.IsValid)
            {
                var result = userService.Register(model);
                if (result != null)
                {
                    return RedirectToAction("VerifyUser");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email Already exist !");
                    return View(model);
                }
            }


            return View(model);
        }

        public IActionResult VerifyUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyUser(string Otp)
        {
            if (Otp != null)
            {
                VerifyAccountEnum result = userService.VerifyAccount(Otp);
                if (result == VerifyAccountEnum.OTPVERIFIED)
                {

                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid OTP !");
                    return View();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please enter OTP");
                return View();
            }
        }
        public IActionResult Forget()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Forget(Forget forget)
        {
            if (forget != null)
            {
                var result = _context.Users.Where(x => x.Email == forget.Email).FirstOrDefault();
                if (result != null)
                {
                    var token = Guid.NewGuid();
                    //var url = _config["UrlConfig:ApplicationURL"] ""result.Username + "/Home/Forget/?Token=" + token = "&Email=" + result.Email;
                    var url = _config["UrlConfig:ApplicationURL"] + "/Account/ResetPassword/?Token=" + token + "&Email=" + result.Email;
                    //var url1 = _config.GetSection("UrlConfig").GetSection("ApplicationURL").Value;
                    result.ResetToken = Convert.ToString(token);
                    _context.SaveChanges();

                    //var body = "Hi" + result.Username + "<span>Click here :" + UrlEncoder.Default.Encode(url) + "to reset password</span><body/>";
                    var body = "Hi " + result.FirstName + ", <br/><br/> click <a class='text-info' style='color:lightblue;' href='" + HtmlEncoder.Default.Encode(url) + "'>here:</a> to reset your password";

                    SendEmail(result.Email, body);

                    ViewBag.LinkSentSuccessfully = "Link has been send successfully to registered email id";
                }
                else
                {
                    ViewBag.NotExists = "User not exists with us!";
                }

            }
            return View();
        }


        [HttpGet]
        public IActionResult ResetPassword(string Token, string Email)
        {
            ResetPasswordModel resetpwd = new ResetPasswordModel();
            resetpwd.Token = Token;
            resetpwd.Email = Email;
            return View(resetpwd);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            if (ModelState.IsValid)
            {
                if (resetPasswordModel != null)
                {
                    var user = _context.Users.Where(a => a.ResetToken == resetPasswordModel.Token && a.Email == resetPasswordModel.Email).FirstOrDefault();
                    if (user != null)
                    {
                        user.Password = resetPasswordModel.Password;
                        user.ResetToken = "";
                        _context.SaveChanges();
                        ViewBag.Msg = "Password Reset Successfully";
                        return RedirectToAction("ResetPasswordConfirmation");
                       
                        
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Data is not correct");
                }
            }

            return View();

        }
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //send mail 
        public void SendEmail(string emailAddress, string body)
            {
                using (MailMessage mm = new MailMessage("verifyotp9@gmail.com", emailAddress))
                {
                    mm.Subject = "Reset Password";
                    mm.Body = body;
                    mm.IsBodyHtml = true;
                    SmtpClient smtpClient = new SmtpClient();
                    smtpClient.Host = "smtp.gmail.com";
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Credentials = new NetworkCredential("verifyotp9@gmail.com", "mjwaixbdrmwtxhtl");    //Pasword: Test123@
                    smtpClient.Send(mm);
                    smtpClient.Dispose();
                }
            }









        }
    }
