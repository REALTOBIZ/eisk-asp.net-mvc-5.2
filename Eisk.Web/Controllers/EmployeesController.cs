﻿/************************************************************************************************************************************************
*   Designed and Implemented By: Ashraf Alam, Microsoft MVP | Github: https://github.com/ashrafalam   | Blog: http://weblogs.asp.net/ashraful   *                
*************************************************************************************************************************************************/

using System;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Eisk.DataAccess;
using Eisk.Helpers;
using Eisk.Models;

namespace Eisk.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly DatabaseContext _dbContext;

        public EmployeesController(DatabaseContext databaseContext)
        {
            _dbContext = databaseContext;
        }

        public ViewResult Index()
        {
            var employees = _dbContext.EmployeeRepository;

            return View(employees.ToArray());
        }

        public ViewResult Listing()
        {
            return View(new GridViewModel(this, _dbContext.EmployeeRepository.Count(), "GridData"));
        }

        public ActionResult Details(int id)
        {
            var employee = _dbContext.EmployeeRepository.Find(id);

            if (employee == null)
            {
                this.ShowMessage("Sorry no employee found with id: " + id
                                 + ". You've been redirected to the default page instead.", MessageType.Danger);

                return RedirectToAction("Index");
            }

            return View(new EmployeeViewModel(employee, this));
        }

        public ViewResult Create()
        {
            return View("Edit", new EmployeeEditorModel());
        }

        [HttpPost]
        public ActionResult Create(Employee newEmployee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    LoadEmployeeImageToObject(newEmployee);
                    _dbContext.EmployeeRepository.Add(newEmployee);
                    _dbContext.SaveChanges();
                    this.ShowMessage("Employee created successfully", MessageType.Success);
                    return RedirectToAction("Edit", new {newEmployee.Id});
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        string.Empty, ExceptionDude.FormatMessage(ex));
                }
            }
            return View("Edit", new EmployeeEditorModel(newEmployee));
        }

        public ActionResult Edit(int id)
        {
            var employee = _dbContext.EmployeeRepository.Find(id);

            if (employee == null)
            {
                this.ShowMessage("Sorry no employee found with id: " + id
                                 + ". You've been redirected to the default page instead.", MessageType.Danger);

                return RedirectToAction("Index");
            }

            return View(new EmployeeEditorModel(employee));
        }

        [HttpPost]
        public ActionResult Edit(Employee existingEmployee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    LoadEmployeeImageToObject(existingEmployee);
                    _dbContext.Entry(existingEmployee).State = EntityState.Modified;
                    _dbContext.SaveChanges();
                    this.ShowMessage("Employee saved successfully", MessageType.Success);
                    return RedirectToAction("Details", new {existingEmployee.Id});
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        string.Empty, ExceptionDude.FormatMessage(ex));
                }
            }
            return View(new EmployeeEditorModel(existingEmployee));
        }

        public void Delete(int id)
        {
            var employee = _dbContext.EmployeeRepository.Find(id);
            _dbContext.EmployeeRepository.Remove(employee);
            _dbContext.SaveChanges();
        }

        public void DeleteSelected(int[] ids)
        {
            foreach (var id in ids)
            {
                var employee = _dbContext.EmployeeRepository.Find(id);
                _dbContext.EmployeeRepository.Remove(employee);
            }
            _dbContext.SaveChanges();
        }

        #region Image Upload Related Methods

        public FileContentResult RenderImage(byte[] image)
        {
            if (image == null)
            {
                var img = Image.FromFile(Server.MapPath("~/Images/noimage.png"));
                var ms = new MemoryStream();
                img.Save(ms, ImageFormat.Gif);
                image = ms.ToArray();
            }

            return File(image, "image/png");
        }

        public FileContentResult EmployeeImageFile(int id)
        {
            var employee = _dbContext.EmployeeRepository.Find(id);

            var byteArray = (employee == null || employee.Id == 0 ? null : employee.Photo);

            return RenderImage(byteArray);
        }

        public FileContentResult AjaxImageUpload()
        {
            Thread.Sleep(2000);
            Session["AjaxPhoto"] = GetImageFromUpload();
            return RenderImage((byte[]) Session["AjaxPhoto"]);
        }

        public FileContentResult DiscardUploadededImage(int id)
        {
            Session["AjaxPhoto"] = null;
            return EmployeeImageFile(id);
        }

        private byte[] GetImageFromUpload()
        {
            HttpPostedFileBase postedFile = null;

            if (Request?.Files != null)
                postedFile = Request.Files["imageUpload"];

            if (postedFile == null || postedFile.FileName == string.Empty)
                return null;


            //--Initialise the size of the array
            var file = new byte[postedFile.InputStream.Length];

            //--Create a new BinaryReader and set the InputStream
            //-- for the Images InputStream to the
            //--beginning, as we create the img using a stream.
            var reader =
                new BinaryReader(postedFile.InputStream);
            postedFile.InputStream.Seek(0, SeekOrigin.Begin);

            //--Load the image binary.
            file = reader.ReadBytes((int) postedFile.
                InputStream.Length);
            return file;
        }

        private void LoadEmployeeImageToObject(Employee employee)
        {
            if (Request != null)
            {
                var uploadedImageFromFileControl = GetImageFromUpload();
                var removeImage = Request["removeImage"] == "on";

                //retrieving image file from ajax upload
                if (Session["AjaxPhoto"] != null)
                {
                    employee.Photo = (byte[]) Session["AjaxPhoto"];
                    //Clear Employee Ajax Photo, so that it couldn't make any impact to other employee operation
                    Session["AjaxPhoto"] = null;
                }
                //if ajax upload is found null, try html file input if see ifwe got any content
                else if (uploadedImageFromFileControl != null)
                    employee.Photo = uploadedImageFromFileControl;
                //if RemoveImage is checked, set employee photo as null
                else if (removeImage)
                    employee.Photo = null;
                else if (employee.Id != 0) //load image from db
                {
                    var oldEmployeeData = _dbContext.EmployeeRepository.Find(employee.Id);
                    _dbContext.Entry(oldEmployeeData).State = EntityState.Detached;
                    employee.Photo = oldEmployeeData.Photo;
                }
            }
        }

        #endregion
    }
}