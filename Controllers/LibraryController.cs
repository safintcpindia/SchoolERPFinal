using Microsoft.AspNetCore.Mvc;
using SchoolERP.Net.Models;
using SchoolERP.Net.Services;
using System.Security.Claims;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolERP.Net.Controllers
{
    public class LibraryController : Controller
    {
        private readonly ILibraryService _service;
        private readonly ICompanyService _companyService;
        private readonly IClassService _classService;
        private readonly ISectionService _sectionService;
        private readonly ISessionService _sessionService;
        private readonly IHumanResourceService _hrService;

        public LibraryController(
            ILibraryService service, 
            ICompanyService companyService,
            IClassService classService,
            ISectionService sectionService,
            ISessionService sessionService,
            IHumanResourceService hrService)
        {
            _service = service;
            _companyService = companyService;
            _classService = classService;
            _sectionService = sectionService;
            _sessionService = sessionService;
            _hrService = hrService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value ?? "0");
        private int GetCompanyId() => _companyService.GetUserCurrentCompany(GetUserId()) ?? 0;
        private int GetSessionId() => _sessionService.GetUserCurrentSession(GetUserId()) ?? 0;

        #region Books
        public IActionResult Books()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetBookList(string? search)
        {
            var data = _service.GetBookList(GetCompanyId(), search);
            return Json(new { success = true, data });
        }

        [HttpGet]
        public IActionResult GetBook(int id)
        {
            var data = _service.GetBookById(id);
            return Json(new { success = data != null, data });
        }

        [HttpPost]
        public IActionResult UpsertBook([FromBody] BookUpsertRequest req)
        {
            var companyId = GetCompanyId();
            if (companyId <= 0) return Json(new { success = false, message = "Valid Company ID required." });

            var res = _service.UpsertBook(req, companyId, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteBook(int id)
        {
            var res = _service.DeleteBook(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var res = _service.ToggleBookStatus(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }
        #endregion

        #region Membership
        public IActionResult AddStudents()
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);
            return View();
        }

        public IActionResult AddStaff()
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            ViewBag.Departments = _hrService.GetAllDepartments(companyId, sessionId);
            return View();
        }

        [HttpGet]
        public IActionResult GetMemberList(string type, int? classId, int? sectionId, int? deptId, string? search)
        {
            var data = _service.GetMemberList(type, GetCompanyId(), classId, sectionId, deptId, search);
            return Json(new { success = true, data });
        }

        [HttpGet]
        public IActionResult SearchForMembership(string type, int? classId, int? sectionId, int? deptId, string? search)
        {
            var data = _service.SearchForMembership(type, GetCompanyId(), classId, sectionId, deptId, search);
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult AddMember([FromBody] LibraryMemberUpsertRequest req)
        {
            var res = _service.AddMember(req, GetCompanyId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteMember(int id, int? studentId, int? staffId)
        {
            if (studentId.HasValue || staffId.HasValue)
            {
                // Custom delete via SP update
                var companyId = GetCompanyId();
                var userId = GetUserId();
                var res = ((LibraryService)_service).DeleteMemberEx(id, studentId, staffId, userId);
                return Json(new { success = res.Success, message = res.Message });
            }
            else
            {
                var res = _service.DeleteMember(id, GetUserId());
                return Json(new { success = res.Success, message = res.Message });
            }
        }
        #endregion

        [HttpGet]
        public IActionResult GetSections(int classId)
        {
            var sections = _sectionService.GetSectionsByClass(classId);
            return Json(sections);
        }

        public IActionResult Member()
        {
            return View();
        }

        public IActionResult Issue(int id)
        {
            if (id <= 0) return RedirectToAction("Member");
            ViewBag.LibraryMemberID = id;
            return View();
        }

        [HttpGet]
        public IActionResult GetMemberDetails(int id)
        {
            var data = _service.GetMemberDetails(id, GetCompanyId());
            return Json(new { success = data != null, data });
        }

        [HttpGet]
        public IActionResult GetIssuedBooks(int memberId)
        {
            var data = _service.GetIssuedBooks(memberId, GetCompanyId());
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult IssueBook([FromBody] IssueReturnUpsertRequest req)
        {
            var res = _service.IssueBook(req, GetCompanyId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult ReturnBook(int issueId, DateTime returnDate)
        {
            var res = _service.ReturnBook(issueId, returnDate, GetCompanyId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        public IActionResult ImportBook()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportBook(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please select a CSV file." });

            try
            {
                var companyId = GetCompanyId();
                var userId = GetUserId();

                using var reader = new StreamReader(file.OpenReadStream());
                string? headerLine = await reader.ReadLineAsync();
                if (headerLine == null) return Json(new { success = false, message = "File is empty." });

                var headers = headerLine.Split(',').Select(h => h.Trim().ToLower()).ToList();
                var results = new List<object>();

                while (!reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',').Select(v => v.Trim()).ToList();
                    var req = new BookUpsertRequest();

                    for (int i = 0; i < headers.Count && i < values.Count; i++)
                    {
                        string header = headers[i];
                        string value = values[i];

                        switch (header)
                        {
                            case "book_title": req.BookTitle = value; break;
                            case "book_no": req.BookNo = value; break;
                            case "isbn_no": req.ISBNNo = value; break;
                            case "subject": req.Subject = value; break;
                            case "rack_no": req.RackNo = value; break;
                            case "publish": req.Publisher = value; break;
                            case "author": req.Author = value; break;
                            case "qty": req.TotalQty = int.TryParse(value, out var qty) ? qty : 0; break;
                            case "perunitcost": req.BookPrice = decimal.TryParse(value, out var price) ? price : 0; break;
                            case "postdate": req.PostDate = DateTime.TryParse(value, out var date) ? date : (DateTime?)null; break;
                            case "description": req.Description = value; break;
                        }
                    }

                    var res = _service.UpsertBook(req, companyId, userId);
                    results.Add(new
                    {
                        bookTitle = req.BookTitle,
                        success = res.Success,
                        message = res.Message
                    });
                }
                return Json(new { success = true, results });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
