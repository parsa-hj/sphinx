﻿namespace Dsp.Areas.Edu.Controllers
{
    using Amazon.S3;
    using Amazon.S3.Model;
    using Entities;
    using global::Dsp.Controllers;
    using Microsoft.AspNet.Identity;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Configuration;
    using System.Web.Mvc;

    [Authorize(Roles = "Pledge, Neophyte, Active, Administrator")]
    public class ClassesController : BaseController
    {
        public async Task<ActionResult> Index(string message)
        {
            ViewBag.Message = string.Empty;

            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.Message = message;
            }
            var classes = await _db.Classes.OrderBy(c => c.CourseShorthand).ToListAsync();
            var model = new ClassIndexModel
            {
                Classes = classes, 
                CurrentSemester = await GetThisSemesterAsync()
            };
            return View(model);
        }

        public async Task<ActionResult> Create()
        {
            ViewBag.Message = string.Empty;
            ViewBag.DepartmentId = new SelectList(await _db.Departments.OrderBy(c => c.Name).ToListAsync(), 
                "DepartmentId", "Name");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Class @class)
        {
            ViewBag.Message = string.Empty;
            if (ModelState.IsValid)
            {
                if (await _db.Classes.AnyAsync(c => c.CourseShorthand == @class.CourseShorthand && c.DepartmentId == @class.DepartmentId))
                {
                    ViewBag.Message = "A Class in that department with that number already exists.";
                }
                else
                {
                    _db.Classes.Add(@class);
                    await _db.SaveChangesAsync();
                    ViewBag.Message = "Class created successfully. ";
                }
            }

            ViewBag.DepartmentId = new SelectList(_db.Departments, "DepartmentId", "Name", @class.DepartmentId);
            return View(@class);
        }

        public async Task<ActionResult> Details(int? id, ClassesMessageId? message)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var course = await _db.Classes.FindAsync(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            switch (message)
            {
                case ClassesMessageId.UploadInvalidFailure:
                case ClassesMessageId.UploadInvalidFileTypeFailure:
                case ClassesMessageId.DownloadFileAwsFailure:
                case ClassesMessageId.DeleteFileAwsFailure:
                    ViewBag.FailMessage = GetResultMessage(message);
                    break;
                case ClassesMessageId.UploadFileSuccess:
                case ClassesMessageId.DeleteFileSuccess:
                    ViewBag.SuccessMessage = GetResultMessage(message);
                    break;
            }

            var model = new ClassDetailsModel
            {
                Class = course,
                CurrentSemester = await GetThisSemesterAsync(),
                FileInfoModel = new FileInfoModel
                {
                    ExistingFiles = new List<string> { "tests.pdf", "homework.pdf", "notes.pdf" }
                }
            };
            return View(model);
        }

        public async Task<ActionResult> Upvote(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = await _db.ClassFiles.FindAsync(id);
            if (file == null)
            {
                return HttpNotFound();
            }

            var existingVote = await _db.ClassFileVotes
                    .SingleOrDefaultAsync(v =>
                        v.UserId == User.Identity.GetUserId<int>() &&
                        v.ClassFileId == file.ClassFileId);

            if (existingVote == null)
            {
                _db.ClassFileVotes.Add(new ClassFileVote
                {
                    UserId = User.Identity.GetUserId<int>(),
                    ClassFileId = file.ClassFileId,
                    IsUpvote = true
                });
            }
            else
            {
                if (existingVote.IsUpvote)
                {
                    _db.Entry(existingVote).State = EntityState.Deleted;
                }
                else
                {
                    existingVote.IsUpvote = true;
                    _db.Entry(existingVote).State = EntityState.Modified;
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = file.ClassId });
        }

        public async Task<ActionResult> Downvote(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = await _db.ClassFiles.FindAsync(id);
            if (file == null)
            {
                return HttpNotFound();
            }

            var existingVote = await _db.ClassFileVotes
                    .SingleOrDefaultAsync(v =>
                        v.UserId == User.Identity.GetUserId<int>() &&
                        v.ClassFileId == file.ClassFileId);

            if (existingVote == null)
            {
                _db.ClassFileVotes.Add(new ClassFileVote
                {
                    UserId = User.Identity.GetUserId<int>(),
                    ClassFileId = file.ClassFileId,
                    IsUpvote = false
                });
            }
            else
            {
                if (!existingVote.IsUpvote)
                {
                    _db.Entry(existingVote).State = EntityState.Deleted;
                }
                else
                {
                    existingVote.IsUpvote = false;
                    _db.Entry(existingVote).State = EntityState.Modified;
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", new { id = file.ClassId });
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            var @class = await _db.Classes.FindAsync(id);
            if (@class == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(await _db.Departments.OrderBy(c => c.Name).ToListAsync(), 
                "DepartmentId", "Name", 
                @class.DepartmentId);
            return View(@class);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Class @class)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(@class).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.DepartmentId = new SelectList(_db.Departments, "DepartmentId", "Name", @class.DepartmentId);
            return View(@class);
        }

        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var @class = await _db.Classes.FindAsync(id);
            if (@class == null)
            {
                return HttpNotFound();
            }

            if (@class.ClassesTaken.Any())
            {
                return RedirectToAction("Index", new
                {
                    message = "This class is currently being taken, or has been taken, " +
                              "by one or more people, therefore it can't be deleted."
                });
            }

            return View(@class);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var @class = await _db.Classes.FindAsync(id);
            if (@class.ClassesTaken.Any())
            {
                return RedirectToAction("Index", new
                {
                    message = "This class is currently being taken, or has been taken, " +
                              "by one or more people, therefore it can't be deleted."
                });
            }
            _db.Entry(@class).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", new { message = "Course deleted." });
        }
        
        public async Task<ActionResult> Schedule(string userName, ClassesMessageId? message)
        {
            ViewBag.Message = string.Empty;

            if (string.IsNullOrEmpty(userName))
            {
                ViewBag.FailMessage = "No one by the username was found!";
                return View(new List<ClassTaken>());
            }

            switch (message)
            {
                case ClassesMessageId.AddClassDuplicateFailure:
                case ClassesMessageId.AddClassUnknownFailure:
                case ClassesMessageId.DeleteClassTakenFailure:
                case ClassesMessageId.EditClassTakenFailure:
                    ViewBag.FailMessage = GetResultMessage(message);
                    break;
                case ClassesMessageId.AddClassSuccess:
                case ClassesMessageId.DeleteClassTakenSuccess:
                case ClassesMessageId.EditClassTakenSuccess:
                    ViewBag.SuccessMessage = GetResultMessage(message);
                    break;
            }

            var member = await UserManager.FindByNameAsync(userName);

            var model = new ClassScheduleModel
            {
                SelectedUserName = userName,
                Member = member,
                AllClasses = await _db.Classes.OrderBy(c => c.CourseShorthand).ToListAsync(),
                Semesters = await GetSemesterListAsync(),
                ClassTaken = new ClassTaken
                {
                    SemesterId = (await GetThisSemesterAsync()).SemesterId,
                    UserId = member.Id
                },
                ClassesTaken = member.ClassesTaken.OrderByDescending(s => s.Semester.DateStart)
            };

            if(!model.ClassesTaken.Any())
            {
                ViewBag.Message += "No classes found for " + userName + ". ";
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> AddClassTaken(ClassScheduleModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Schedule", new
                {
                    userName = model.SelectedUserName, 
                    message = ClassesMessageId.AddClassUnknownFailure
                });

            if (!User.IsInRole("Academics") && !User.IsInRole("Administrator") &&
                User.Identity.GetUserName() != model.SelectedUserName)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var member = await UserManager.FindByNameAsync(model.SelectedUserName);

            if (member.ClassesTaken.Any(c => 
                c.ClassId == model.ClassTaken.ClassId && 
                c.UserId == model.ClassTaken.UserId && 
                c.SemesterId == model.ClassTaken.SemesterId))
            {
                return RedirectToAction("Schedule", new
                {
                    userName = model.SelectedUserName,
                    message = ClassesMessageId.AddClassDuplicateFailure
                });
            }

            _db.ClassesTaken.Add(model.ClassTaken);
            await _db.SaveChangesAsync();

            return RedirectToAction("Schedule", new
            {
                userName = model.SelectedUserName, 
                message = ClassesMessageId.AddClassSuccess
            });
        }

        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> DeleteFromSchedule(int id, int sid, int cid, string username)
        {
            var entry = await _db.ClassesTaken.SingleAsync(c => c.UserId == id && c.SemesterId == sid && c.ClassId == cid);
            if (entry == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            return View(entry);
        }

        [HttpPost, ActionName("DeleteFromSchedule"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> DeleteConfirmed(ClassTaken model)
        {
            var entry = await _db.ClassesTaken.SingleAsync(c => c.UserId == model.UserId && c.SemesterId == model.SemesterId && c.ClassId == model.ClassId);
            var member = await UserManager.FindByIdAsync(model.UserId);
            if (entry == null)
            {
                return RedirectToAction("Schedule", new
                {
                    userName = member.UserName ?? User.Identity.GetUserName(),
                    message = ClassesMessageId.DeleteClassTakenFailure
                });
            }

            _db.Entry(entry).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return RedirectToAction("Schedule", new
            {
                userName = member.UserName ?? User.Identity.GetUserName(), 
                message = ClassesMessageId.DeleteClassTakenSuccess
            });
        }

        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> EditClassTaken(int id, int sid, int cid)
        {
            var model = await _db.ClassesTaken.SingleAsync(c => c.UserId == id && c.SemesterId == sid && c.ClassId == cid);
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> EditClassTaken(ClassTaken classTaken)
        {
            if (!ModelState.IsValid) 
                return RedirectToAction("Schedule", new
                {
                    userName = classTaken.Member.UserName,
                    message = ClassesMessageId.EditClassTakenFailure
                });

            _db.Entry(classTaken).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            var member = await UserManager.FindByIdAsync(classTaken.UserId);

            return RedirectToAction("Schedule", new { 
                userName = member.UserName,
                message = ClassesMessageId.EditClassTakenSuccess 
            });
        }

        public async Task<ActionResult> Transcript(string userName, ClassesMessageId? message)
        {
            if (!User.IsInRole("Administrator") && !User.IsInRole("Academics") && User.Identity.GetUserName() != userName)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            
            switch(message)
            {
                case ClassesMessageId.UpdateTranscriptFailure:
                    ViewBag.FailMessage = GetResultMessage(message);
                    break;
                case ClassesMessageId.UpdateTranscriptSuccess:
                    ViewBag.SuccessMessage = GetResultMessage(message);
                    break;
            }

            var member = await UserManager.Users.SingleAsync(m => m.UserName == userName);
            var model = new List<ClassTranscriptModel>();
            foreach (var c in member.ClassesTaken
                .OrderByDescending(c => c.Semester.DateStart)
                .ThenBy(c => c.Class.CourseShorthand))
            {
                model.Add(new ClassTranscriptModel
                {
                    Member = member,
                    ClassTaken = c,
                    Grades = GetGrades()
                });
            }

            ViewBag.UserName = userName;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Transcript(IList<ClassTranscriptModel> model)
        {
            if (!User.IsInRole("Administrator") && !User.IsInRole("Academics") &&
                !model.Any() && User.Identity.GetUserName() != model.First().Member.UserName)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            foreach(var c in model)
            {
                var c1 = c;
                var classTaken = await _db.ClassesTaken
                    .SingleAsync(t => 
                        t.ClassId == c1.ClassTaken.ClassId &&
                        t.SemesterId == c1.ClassTaken.SemesterId &&
                        t.UserId == c1.ClassTaken.UserId);

                classTaken.MidtermGrade = c.ClassTaken.MidtermGrade;
                classTaken.FinalGrade = c.ClassTaken.FinalGrade;
                classTaken.Dropped = c.ClassTaken.Dropped;
                classTaken.IsSummerClass = c.ClassTaken.IsSummerClass;
                _db.Entry(classTaken).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Transcript", new
            {
                userName = model.First().Member.UserName, 
                message = ClassesMessageId.UpdateTranscriptSuccess
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadFile(ClassDetailsModel model)
        {
            if (model.FileInfoModel.File == null || model.FileInfoModel.File.ContentLength <= 0) 
                return RedirectToAction("Details", new
                {
                    id = model.Class.ClassId,
                    message = ClassesMessageId.UploadInvalidFailure
                });
            if (model.FileInfoModel.File.ContentType != "application/pdf") 
                return RedirectToAction("Details", new
                {
                    id = model.Class.ClassId,
                    message = ClassesMessageId.UploadInvalidFileTypeFailure
                });

            var awsAccessKey = WebConfigurationManager.AppSettings["AWSAccessKey"];
            var awsSecretKey = WebConfigurationManager.AppSettings["AWSSecretKey"];
            var awsBucket = WebConfigurationManager.AppSettings["AWSFileBucket"];

            try
            {
                IAmazonS3 client;
                var key = string.Format(model.Class.CourseShorthand + "/{0}", model.FileInfoModel.File.FileName);
                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(awsAccessKey, awsSecretKey))
                {
                    var request = new PutObjectRequest()
                    {
                        BucketName = awsBucket,
                        CannedACL = S3CannedACL.Private,
                        Key = key,
                        InputStream = model.FileInfoModel.File.InputStream
                    };

                    client.PutObject(request);
                }

                var file = new ClassFile
                {
                    ClassId = model.Class.ClassId,
                    UserId = User.Identity.GetUserId<int>(),
                    AwsCode = key, 
                    UploadedOn = DateTime.UtcNow,
                };
                _db.ClassFiles.Add(file);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {

            }

            return RedirectToAction("Details", new
            {
                id = model.Class.ClassId,
                message = ClassesMessageId.UploadFileSuccess
            });
        }

        public async Task<ActionResult> DownloadFile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            var file = await _db.ClassFiles.FindAsync(id);
            if (file == null)
            {
                return HttpNotFound();
            }

            try
            {
                var awsAccessKey = WebConfigurationManager.AppSettings["AWSAccessKey"];
                var awsSecretKey = WebConfigurationManager.AppSettings["AWSSecretKey"];
                var awsBucket = WebConfigurationManager.AppSettings["AWSFileBucket"];
                IAmazonS3 client;
                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(awsAccessKey, awsSecretKey))
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = awsBucket, 
                        Key = file.AwsCode
                    };
                    // Make AWS Request for file.
                    var response = client.GetObject(request);

                    using(var memoryStream = new MemoryStream())
                    {
                        // Convert response to a readable format.
                        response.ResponseStream.CopyTo(memoryStream);
                        var contents = memoryStream.ToArray();
                        var fileName = file.AwsCode.Split('/').Last();

                        // Update file access time in database.
                        file.LastAccessedOn = DateTime.UtcNow;
                        file.DownloadCount++;
                        _db.Entry(file).State = EntityState.Modified;
                        await _db.SaveChangesAsync();

                        // Return file to user's browser.
                        return File(contents, "application/pdf", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", new
                {
                    id = file.Class.ClassId,
                    message = ClassesMessageId.DownloadFileAwsFailure
                });
            }
        }

        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> DeleteFile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            var file = await _db.ClassFiles.FindAsync(id);
            if (file == null)
            {
                return HttpNotFound();
            }

            return View(file);
        }

        [HttpPost, ActionName("DeleteFile"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Academics")]
        public async Task<ActionResult> DeleteFileConfirmed(int id, int classId)
        {
            var file = await _db.ClassFiles.SingleAsync(f => f.ClassFileId == id);
            if (file == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            try
            {
                var awsAccessKey = WebConfigurationManager.AppSettings["AWSAccessKey"];
                var awsSecretKey = WebConfigurationManager.AppSettings["AWSSecretKey"];
                var awsBucket = WebConfigurationManager.AppSettings["AWSFileBucket"];
                IAmazonS3 client;
                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(awsAccessKey, awsSecretKey))
                {
                    var request = new DeleteObjectRequest
                    {
                        BucketName = awsBucket,
                        Key = file.AwsCode
                    };
                    client.DeleteObject(request);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", new
                {
                    id = classId,
                    message = ClassesMessageId.DeleteFileAwsFailure
                });
            }

            foreach (var v in file.ClassFileVotes.ToList())
            {
                _db.Entry(v).State = EntityState.Deleted;
            }

            _db.Entry(file).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new
            {
                id = classId,
                message = ClassesMessageId.DeleteFileSuccess
            });
        }

        public static dynamic GetResultMessage(ClassesMessageId? message)
        {
            return message == ClassesMessageId.UpdateTranscriptFailure ? "Failed to update transcript for unknown reason, please contact your administrator."
                : message == ClassesMessageId.UpdateTranscriptSuccess ? "Transcript update was successful."
                : message == ClassesMessageId.UploadInvalidFailure ? "Could not upload file because there was nothing selected to upload."
                : message == ClassesMessageId.UploadInvalidFileTypeFailure ? "Could not upload file because it is not a properly formatted PDF."
                : message == ClassesMessageId.UploadFileSuccess ? "File was uploaded successfully!"
                : message == ClassesMessageId.DownloadFileAwsFailure ? "Could not download file because of a server error.  If the problem persists, please contact your administrator."
                : message == ClassesMessageId.DeleteFileAwsFailure ? "Could not delete file because of a server error.  If the problem persists, please contact your administrator."
                : message == ClassesMessageId.DeleteFileSuccess ? "File was deleted successfully!"
                : message == ClassesMessageId.AddClassDuplicateFailure ? "You can't add the same class twice in one semester!"
                : message == ClassesMessageId.AddClassUnknownFailure ? "Failed to add a class for some unknown reason, contact your administrator."
                : message == ClassesMessageId.AddClassSuccess ? "Class enrollment was successful!"
                : message == ClassesMessageId.DeleteClassTakenFailure ? "Could not find the class to remove for this member.  Please try again or contact your administrator if the problem persists."
                : message == ClassesMessageId.DeleteClassTakenSuccess ? "Class successfully removed."
                : message == ClassesMessageId.EditClassTakenFailure ? "Failed to update class because "
                : message == ClassesMessageId.EditClassTakenSuccess ? "Enrollment information for class was updated successfully."
                : "";
        }

        public enum ClassesMessageId
        {
            UpdateTranscriptFailure,
            UpdateTranscriptSuccess,
            UploadInvalidFailure,
            UploadInvalidFileTypeFailure,
            UploadFileSuccess,
            DownloadFileAwsFailure,
            DeleteFileAwsFailure,
            DeleteFileSuccess,
            AddClassDuplicateFailure,
            AddClassUnknownFailure,
            AddClassSuccess,
            DeleteClassTakenSuccess,
            DeleteClassTakenFailure,
            EditClassTakenSuccess,
            EditClassTakenFailure
        }
    }
}
