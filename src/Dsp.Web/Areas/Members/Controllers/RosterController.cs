﻿namespace Dsp.Web.Areas.Members.Controllers
{
    using Dsp.Web.Controllers;
    using Dsp.Data.Entities;
    using Models;
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Mvc;

    [Authorize(Roles = "Pledge, Neophyte, Active, Alumnus, Affiliate")]
    public class RosterController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index(RosterFilterModel filter)
        {
            Semester semester;
            if (filter.sem == null)
            {
                semester = await base.GetThisSemesterAsync();
                filter.sem = semester.SemesterId;
            }
            else
            {
                semester = await _db.Semesters.FindAsync(filter.sem);
            }

            var members = await base.GetRosterForSemester(semester);
            ViewBag.Sort = filter.sort;
            ViewBag.Order = filter.order;
            ViewBag.SearchTerm = filter.s;
            var filteredResults = base.GetFilteredMembersList(members, filter.s, filter.sort, filter.order);
            
            var model = new RosterIndexModel
            {
                 SelectedSemester = semester.SemesterId,
                 Semester = semester,
                 Semesters = await base.GetSemesterListAsync(),
                 Members = filteredResults
            };

            return View(model);
        }
        
        [HttpGet, Authorize(Roles = "Administrator, Secretary")]
        public async Task<ActionResult> InitiatePledges(string message)
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];

            var model = new InitiatePledgesModel
            {
                Pledges = await GetPledgeUserIdListAsFullNameAsync()
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrator, Secretary")]
        public async Task<ActionResult> InitiatePledges(InitiatePledgesModel model)
        {
            var pledges = await _db.Users
                .Where(m => 
                    model.SelectedMemberIds.Contains(m.Id))
                .ToListAsync();
            var activeId = (await _db.MemberStatuses.SingleAsync(s => s.StatusName == "Active")).StatusId;

            foreach(var p in pledges)
            {
                p.StatusId = activeId;
                _db.Entry(p).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pledges successfully moved to active status.";
            return RedirectToAction("InitiatePledges");
        }
        
        [HttpGet, Authorize(Roles = "Administrator, Secretary")]
        public async Task<ActionResult> GraduateActives(string message)
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];

            var model = new GraduateActivesModel
            {
                Actives = await GetGraduatingActiveUserIdListAsFullNameAsync()
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrator, Secretary")]
        public async Task<ActionResult> GraduateActives(GraduateActivesModel model)
        {
            var actives = await _db.Users
                .Where(m =>
                    model.SelectedMemberIds.Contains(m.Id))
                .ToListAsync();
            var alumnusId = (await _db.MemberStatuses.SingleAsync(s => s.StatusName == "Alumnus")).StatusId;

            foreach (var p in actives)
            {
                p.StatusId = alumnusId;
                _db.Entry(p).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Actives successfully moved to alumni status.";
            return RedirectToAction("GraduateActives");
        }

        [HttpGet]
        public async Task<FileContentResult> Download()
        {
            var members = await UserManager.Users
                .OrderBy(m => m.MemberStatus.StatusId)
                .ThenBy(m => m.LastName)
                .ToListAsync();
            const string header = "First Name, Last Name, Mobile, Email, Member Status, Pledge Class, Pin, Graduation, Room, Big Bro, T-Shirt";
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var m in members)
            {
                var phone = m.PhoneNumbers.SingleOrDefault(p => p.Type == "Mobile");
                var line = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                    m.FirstName,
                    m.LastName,
                    phone == null ? "None" : phone.Number,
                    m.Email,
                    m.MemberStatus.StatusName,
                    m.PledgeClass.PledgeClassName,
                    m.Pin,
                    m.GraduationSemester.ToString(),
                    m.RoomString(),
                    m.BigBrother == null ? "None" : m.BigBrother.FirstName + " " + m.BigBrother.LastName,
                    m.ShirtSize);
                sb.AppendLine(line);
            }

            return File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "dsp-roster.csv");
        }
    }
}