using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student4.Data;
using Student4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Xrm.Sdk;

namespace Student4.Controllers
{
    [Authorize]
    public class InquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InquiriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;    
        }

        // GET: Inquiries
        public async Task<IActionResult> Index()
        {
            var inquiries = _context.Inquiry.Where(i => i.UserId == _userManager.GetUserId(User));
            return View(await _context.Inquiry.ToListAsync());
        }

        // GET: Inquiries/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inquiry = await _context.Inquiry
                .SingleOrDefaultAsync(m => m.Inquiryid == id);

            var service = CRM.CrmService.GetServiceProvider();

            Entity crmInquiry = service.Retrieve("sd_inquiry", inquiry.Inquiryid, new Microsoft.Xrm.Sdk.Query.ColumnSet("sd_response"));

            inquiry.Response = crmInquiry.GetAttributeValue<string>("sd_response");

            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        // GET: Inquiries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inquiries/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Inquiryid,Question,Response,UserId")] Inquiry inquiry)
        {
            if (ModelState.IsValid)
            {
                inquiry.Inquiryid = Guid.NewGuid();
                _context.Add(inquiry);
                await _context.SaveChangesAsync();

                Entity crmInquiry = new Entity("sd_inquiry");
                crmInquiry.Id = inquiry.Inquiryid;
                crmInquiry["sd_question"] = inquiry.Question;
                crmInquiry["sd_response"] = inquiry.Response;
                crmInquiry["sd_contact"] = new EntityReference("contact", Guid.Parse(_userManager.GetUserId(User)));

                var service = CRM.CrmService.GetServiceProvider();
                service.Create(crmInquiry);

                return RedirectToAction("Index");
            }
            return View(inquiry);
        }

        // GET: Inquiries/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.Inquiryid == id);
            if (inquiry == null)
            {
                return NotFound();
            }
            return View(inquiry);
        }

        // POST: Inquiries/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Inquiryid,Question,Response,UserId")] Inquiry inquiry)
        {
            if (id != inquiry.Inquiryid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    inquiry.UserId = _userManager.GetUserId(User);
                    _context.Update(inquiry);
                    await _context.SaveChangesAsync();

                    var service = CRM.CrmService.GetServiceProvider();

                    Entity crmEntity = service.Retrieve("sd_inquiry", inquiry.Inquiryid, new Microsoft.Xrm.Sdk.Query.ColumnSet("sd_question", "sd_response"));
                    crmEntity["sd_question"] = inquiry.Question;
                    crmEntity["sd_response"] = inquiry.Response;

                    service.Update(crmEntity);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InquiryExists(inquiry.Inquiryid))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(inquiry);
        }

        // GET: Inquiries/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inquiry = await _context.Inquiry
                .SingleOrDefaultAsync(m => m.Inquiryid == id);
            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        // POST: Inquiries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var inquiry = await _context.Inquiry.SingleOrDefaultAsync(m => m.Inquiryid == id);
            _context.Inquiry.Remove(inquiry);
            await _context.SaveChangesAsync();

            var service = CRM.CrmService.GetServiceProvider();
            service.Delete("sd_inquiry", id);

            return RedirectToAction("Index");
        }

        private bool InquiryExists(Guid id)
        {
            return _context.Inquiry.Any(e => e.Inquiryid == id);
        }
    }
}
