﻿namespace DeltaSigmaPhiWebsite.Areas.Service.Controllers
{
    using DeltaSigmaPhiWebsite.Controllers;
    using Entities;
    using Models;
    using System.Data.Entity;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using WebMatrix.WebData;

    [Authorize(Roles = "Neophyte, Pledge, Active, Administrator")]
    public class EventsController : BaseController
    {
        public async Task<ActionResult> Index(EventIndexFilterModel model, EventMessageId? message)
        {
            if (model.SelectedSemester == null)
            {
                model.SelectedSemester = await GetThisSemestersIdAsync();
            }
            switch (message)
            {
                case EventMessageId.DeleteNotEmptyFailure:
                    ViewBag.FailMessage = GetResultMessage(message);
                    break;
                case EventMessageId.CreateSuccess:
                case EventMessageId.EditSuccess:
                case EventMessageId.DeleteSuccess:
                    ViewBag.SuccessMessage = GetResultMessage(message);
                    break;
            }

            var thisSemester = await _db.Semesters.FindAsync(model.SelectedSemester);
            var previousSemester = (await _db.Semesters
                .Where(s => s.DateEnd < thisSemester.DateStart)
                .OrderBy(s => s.DateEnd).ToListAsync()).LastOrDefault() ?? new Semester
                {
                    // In case they pick the very first semester in the system.
                    DateEnd = thisSemester.DateStart
                };

            model.Events = await _db.Events
                .Where(e => e.DateTimeOccurred < thisSemester.DateEnd && 
                            e.DateTimeOccurred >= previousSemester.DateEnd)
                .ToListAsync();
            model.SemesterList = await GetSemesterListAsync();

            return View(model);
        }
        
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Event @event)
        {
            if (!ModelState.IsValid) return View(@event);

            if(User.IsInRole("Administrator") || User.IsInRole("Service"))
            {
                @event.IsApproved = true;
            }
            else
            {
                @event.IsApproved = false;
                // TODO: Email service chairman.
            }
            @event.SubmitterId = WebSecurity.CurrentUserId;
            @event.DateTimeOccurred = ConvertCstToUtc(@event.DateTimeOccurred);
            _db.Events.Add(@event);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", new
            {
                SelectedSemester = (await GetSemestersForUtcDateAsync(@event.DateTimeOccurred)).SemesterId,
                message = EventMessageId.CreateSuccess
            });
        }

        [Authorize(Roles = "Administrator, Service")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var @event = await _db.Events.FindAsync(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            @event.DateTimeOccurred = base.ConvertUtcToCst(@event.DateTimeOccurred);
            ViewBag.Semester = (await GetSemestersForUtcDateAsync(@event.DateTimeOccurred)).SemesterId;
            return View(@event);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Service")]
        public async Task<ActionResult> Edit(Event @event)
        {
            if (!ModelState.IsValid) return View(@event);

            @event.DateTimeOccurred = ConvertCstToUtc(@event.DateTimeOccurred);
            _db.Entry(@event).State = EntityState.Modified;
            await _db.SaveChangesAsync(); 
            
            return RedirectToAction("Index", new
            {
                SelectedSemester = (await GetSemestersForUtcDateAsync(@event.DateTimeOccurred)).SemesterId,
                message = EventMessageId.EditSuccess
            });
        }
        
        [Authorize(Roles = "Administrator, Service")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var @event = await _db.Events.FindAsync(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            var semester = await GetSemestersForUtcDateAsync(@event.DateTimeOccurred);
            if (@event.ServiceHours.Any())
            {
                return RedirectToAction("Index", new
                {
                    SelectedSemester = semester.SemesterId,
                    message = EventMessageId.DeleteNotEmptyFailure
                });
            }
            @event.DateTimeOccurred = base.ConvertUtcToCst(@event.DateTimeOccurred);

            ViewBag.Semester = semester.SemesterId;
            return View(@event);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Service")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var @event = await _db.Events.FindAsync(id);
            _db.Events.Remove(@event);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", new
            {
                SelectedSemester = (await GetSemestersForUtcDateAsync(@event.DateTimeOccurred)).SemesterId,
                message = EventMessageId.DeleteSuccess
            });
        }

        public static dynamic GetResultMessage(EventMessageId? message)
        {
            return message == EventMessageId.DeleteNotEmptyFailure ? "Failed to delete event because someone has already turned in hours for it."
                : message == EventMessageId.CreateSuccess ? "Event was created successfully."
                : message == EventMessageId.EditSuccess ? "Event was updated successfully."
                : message == EventMessageId.DeleteSuccess ? "Event was deleted successfully."
                : "";
        }

        public enum EventMessageId
        {
            DeleteNotEmptyFailure,
            CreateSuccess,
            EditSuccess,
            DeleteSuccess
        }
    }
}