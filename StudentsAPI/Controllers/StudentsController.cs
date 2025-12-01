using StudentsAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace StudentsAPI.Controllers
{
    public class StudentsController : ApiController
    {
        private AndroidEntities db = new AndroidEntities();
        private readonly string appDataPath = HttpContext.Current.Server.MapPath("~/Uploads/");
        private readonly string uploadsPath;
        private StudentsController()
        {
            uploadsPath = Path.Combine(appDataPath, "Students");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
        }


        // GET: api/Students
        public IHttpActionResult GetStudent()
        {
            try
            {
                var students = db.Student;
                foreach (var s in students)
                {
                    if (!string.IsNullOrEmpty(s.ImagePath))
                    {
                        //generate base Url that is attched with the path comes from DB
                        var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority) + Request.GetRequestContext().VirtualPathRoot.TrimEnd('/');
                        var url = baseUrl + s.ImagePath;
                        s.ImagePath = url;
                    }
                }
                return Ok(students);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // GET: api/Students/5
        [ResponseType(typeof(Student))]
        public IHttpActionResult GetStudent(string id)
        {
            Student student = db.Student.Find(id);
            if (student == null)
            {
                return NotFound();
            }

            return Ok(student);
        }

        // PUT: api/Students/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutStudent(string id, Student student)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != student.RegNo)
            {
                return BadRequest();
            }

            db.Entry(student).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Students
        [ResponseType(typeof(Student))]
        public async Task<IHttpActionResult> PostStudent()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Invalid Data Type");
            //Data Provider
            var provider = new MultipartFormDataStreamProvider(uploadsPath);
            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);
                //Read Data
                var form = provider.FormData;
                var name = form.Get("name");
                var regno = form.Get("regno");
                var cgpaStr = form.Get("cgpa");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(regno) || string.IsNullOrEmpty(cgpaStr))
                    return BadRequest("Missing Any Field");
                if (!double.TryParse(cgpaStr, out double cgpa))
                    return BadRequest("CGPA must be a valid number.");
                if (db.Student.Any(s => s.RegNo== regno))
                    return BadRequest($"{regno} Already Exists!!");
                String filename = null;
                if (provider.FileData.Any())
                {
                    var fileData = provider.FileData.First();
                    var realName = fileData.Headers.ContentDisposition.FileName?.Trim('"') ?? "upload.jpg";
                    var ext = Path.GetExtension(realName);
                    filename = Guid.NewGuid().ToString() + ext;
                    var dest = Path.Combine(uploadsPath, filename);
                    File.Move(fileData.LocalFileName, dest);
                }
                if (filename == null)
                    return BadRequest("Image file is required.");
                //Path of Image that store in DB
                var imgPathInDB = "/Uploads/Students/" + filename;
                // Create Student Model
                var student = new Student
                {
                    RegNo = regno,
                    Name = name,
                    CGPA = cgpa,
                    ImagePath = imgPathInDB
                };
                db.Student.Add(student);
                db.SaveChanges();
                return Ok(student);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // DELETE: api/Students/5
        [ResponseType(typeof(Student))]
        public IHttpActionResult DeleteStudent(string id)
        {
            Student student = db.Student.Find(id);
            if (student == null)
            {
                return NotFound();
            }

            db.Student.Remove(student);
            db.SaveChanges();

            return Ok(student);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool StudentExists(string id)
        {
            return db.Student.Count(e => e.RegNo == id) > 0;
        }
    }
}