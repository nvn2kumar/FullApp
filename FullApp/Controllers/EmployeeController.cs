using FullApp.Models;
using FullApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Employee5.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private CustomerContext dbcontext;
        private IHostingEnvironment Environment;
        public EmployeeController(CustomerContext _dbcontext, IHostingEnvironment environment)
        {
            dbcontext = _dbcontext;
            Environment = environment;
        }
        public IActionResult Index()
        {
            var emps = dbcontext.Employees.ToList();
            
            return View(emps);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Employee employee)
        {
           
           var files = Request.Form.Files;
            string dbPath = string.Empty;
            if (files.Count > 0)
            {
                string path = Environment.WebRootPath;
                //string path2 = Environment.ContentRootPath;
                string fullpath = Path.Combine(path, "images", files[0].FileName);

                dbPath = "images/" + files[0].FileName;

                FileStream stream = new FileStream(fullpath, FileMode.Create);
                files[0].CopyTo(stream);

            }
            employee.Image = dbPath;
            dbcontext.Employees.Add(employee);
            dbcontext.SaveChanges();
            return RedirectToAction("Index");
        }
       

        public IActionResult Details(int id)
        {
            var emps = dbcontext.Employees.Where(e => e.Id == id).FirstOrDefault();
            return View(emps);
        }


        public IActionResult Delete(int id)
        {
            var emps = dbcontext.Employees.Where(e => e.Id == id).FirstOrDefault();

            if (emps != null)
            {
                dbcontext.Employees.Remove(emps);
                dbcontext.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index");
            }
           
        }

        public IActionResult Edit(int id)
        {
            var emps = dbcontext.Employees.Where(e => e.Id == id).FirstOrDefault();
            return View(emps);
        }

        [HttpPost]
        public IActionResult Edit(Employee employee)
        {

            var files = Request.Form.Files;
            string dbPath = string.Empty;
            if (files.Count > 0)
            {
                string path = Environment.WebRootPath;
                //string path2 = Environment.ContentRootPath;
                string fullpath = Path.Combine(path, "images", files[0].FileName);

                dbPath = "images/" + files[0].FileName;

                FileStream stream = new FileStream(fullpath, FileMode.Create);
                files[0].CopyTo(stream);

            }
             else
             {
                var emps = dbcontext.Employees.AsNoTracking().Where(e => e.Id == employee.Id).FirstOrDefault();

                dbPath = emps.Image;
             }
            employee.Image = dbPath;

            dbcontext.Employees.Update(employee);
            dbcontext.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
