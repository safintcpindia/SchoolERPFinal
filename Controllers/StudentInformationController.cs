using Microsoft.AspNetCore.Mvc;
using SchoolERP.Net.Services;
using SchoolERP.Net.Models;
using System.Security.Claims;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace SchoolERP.Net.Controllers
{
    /// <summary>
    /// This controller manages all the web pages related to student information.
    /// It handles pages for student admission, viewing student lists, student categories, 
    /// houses, and other student-specific settings.
    /// Think of this as the manager who decides which page to show when you click on student-related menus.
    /// </summary>
    public class StudentInformationController : Controller
    {
        private readonly IStudentInformationService _studentService;
        private readonly ICompanyService _companyService;
        private readonly IFieldService _fieldService;
        private readonly ISessionService _sessionService;
        private readonly IRouteService _routeService;
        private readonly IRoutePickupPointService _routePickupPointService;
        private readonly IHostelService _hostelService;
        private readonly IClassService _classService;
        private readonly ISectionService _sectionService;
        private readonly IVehicleAssignService _vehicleAssignService;
        private readonly IAttendanceService _attendanceService;

        public StudentInformationController(IStudentInformationService studentService, ICompanyService companyService, ISessionService sessionService, IFieldService fieldService, IRouteService routeService, IRoutePickupPointService routePickupPointService, IHostelService hostelService, IClassService classService, ISectionService sectionService, IVehicleAssignService vehicleAssignService, IAttendanceService attendanceService)
        {
            _studentService = studentService;
            _companyService = companyService;
            _sessionService = sessionService;
            _fieldService = fieldService;
            _routeService = routeService;
            _routePickupPointService = routePickupPointService;
            _hostelService = hostelService;
            _classService = classService;
            _sectionService = sectionService;
            _vehicleAssignService = vehicleAssignService;
            _attendanceService = attendanceService;
        }


        private int GetUserId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private int GetCompanyId() => _companyService.GetUserCurrentCompany(GetUserId()) ?? 0;
        private int GetSessionId() => _sessionService.GetUserCurrentSession(GetUserId()) ?? 0;

        /// <summary>
        /// Shows the page where the school can manage reasons for disabling students (e.g., 'Transfer Certificate issued').
        /// </summary>
        public IActionResult DisableReason()
        {
            var model = new StudentDisableReasonPageViewModel
            {
                Items = _studentService.GetAllDisableReasons(GetCompanyId(), GetSessionId())
            };
            return View(model);
        }

        /// <summary>
        /// Shows the page for managing student houses (e.g., Red House, Blue House).
        /// </summary>
        public IActionResult StudentHouse()
        {
            var model = new StudentHousePageViewModel
            {
                Items = _studentService.GetAllStudentHouses(GetCompanyId(), GetSessionId())
            };
            return View(model);
        }

        /// <summary>
        /// Shows the page for managing student categories (e.g., General, OBC).
        /// </summary>
        public IActionResult StudentCategory()
        {
            var model = new StudentCategoryPageViewModel
            {
                Items = _studentService.GetAllStudentCategories(GetCompanyId(), GetSessionId())
            };
            return View(model);
        }

        /// <summary>
        /// Shows the main Student Admission form. 
        /// If an 'id' is provided, it opens the form in 'Edit' mode for that specific student.
        /// It also prepares all the dropdown lists needed for the form, like classes, houses, and hostels.
        /// </summary>
        public IActionResult StudentAdmission(int? id)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            
            // Fetch all fields to ensure Transport, Hostel, and Guardian categories are included
            var fields = _fieldService.GetAllFields(companyId, sessionId, belongsTo: "students");
            
            if (id.HasValue && id > 0)
            {
                var studentDetails = _studentService.GetStudentDetails(id.Value, companyId, sessionId);
                if (studentDetails.BasicInfo.StudentID > 0)
                {
                    ViewBag.StudentDetails = studentDetails;
                    ViewBag.IsEdit = true;
                }
            }
            
            

            // Fetch lookup data for dropdowns
            ViewBag.Categories = _studentService.GetAllStudentCategories(companyId, sessionId);
            ViewBag.Houses = _studentService.GetAllStudentHouses(companyId, sessionId);
            ViewBag.Routes = _routeService.GetAllRoutes(companyId, sessionId);
            ViewBag.PickupPoints = _routePickupPointService.GetAllRoutePickupPoints(companyId, sessionId);
            ViewBag.Hostels = _hostelService.GetAllHostels(companyId, sessionId);
            ViewBag.VehicleAssignments = _vehicleAssignService.GetAllAssignments(companyId, sessionId);
            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);

            // Check if Roll No is Auto Generated
            var autoGenSettings = _fieldService.GetIDAutoGenSettings(companyId, sessionId);
            var studentSettings = autoGenSettings.FirstOrDefault(s => s.EntityType == "Student" && s.IsEnabled);
            bool isRollAutoGenerated = studentSettings != null;
            ViewBag.IsRollAutoGenerated = isRollAutoGenerated;
            if (isRollAutoGenerated)
            {
                ViewBag.RollNoDependencies = studentSettings.FieldsToInclude;
                if (!(ViewBag.IsEdit ?? false))
                {
                    ViewBag.NextRollNo = _studentService.GetNewStudentRollNo(companyId, sessionId);
                }
            }

            return View(fields);
        }

        [HttpGet]
        public IActionResult GetStudentList(int? classId, int? sectionId, string? searchTerm, int? excludeStudentId)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            var students = _studentService.GetStudentList(companyId, sessionId, classId, sectionId, searchTerm);
            
            if (excludeStudentId.HasValue)
            {
                students = students.Where(s => s.StudentID != excludeStudentId.Value).ToList();
            }

            var result = students.Select(s => new { 
                studentID = s.StudentID, 
                fullName = s.FullName, 
                admissionNo = s.AdmissionNo,
                rollNo = s.RollNo
            }).ToList();
            return Json(result);
        }

        [HttpGet]
        [HttpPost]
        /// <summary>
        /// Shows the list of all students. 
        /// Users can filter the list by class, section, or search for a specific name/roll number.
        /// </summary>
        public IActionResult Students(int? classId, int? sectionId, string? search, string? viewType)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            
            var allFields = _fieldService.GetAllFields(companyId, sessionId, belongsTo: "students");
            var model = new StudentListPageViewModel
            {
                Students = _studentService.GetStudentList(companyId, sessionId, classId, sectionId, search),
                SelectedClassId = classId,
                SelectedSectionId = sectionId,
                SearchTerm = search,
                ViewType = viewType ?? "list",
                SystemFields = allFields.Where(f => f.IsSystemField).ToList(),
                CustomFields = allFields.Where(f => !f.IsSystemField).ToList()
            };

            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);
            if (classId.HasValue)
            {
                ViewBag.Sections = _sectionService.GetSectionsByClass(classId.Value);
            }
            else
            {
                ViewBag.Sections = new List<MstSectionViewModel>();
            }

            return View(model);
        }
        [Route("StudentInformation/Details/{id}")]
        /// <summary>
        /// Shows the detailed profile page of a specific student.
        /// </summary>
        public IActionResult Details(int id)
        {
            var model = _studentService.GetStudentDetails(id, GetCompanyId(), GetSessionId());
            if (model.BasicInfo.StudentID == 0) 
            {
                return Content($"Student record with ID {id} was not found for the current Company/Session.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult GetStudentDetails(int id)
        {
            var model = _studentService.GetStudentDetails(id, GetCompanyId(), GetSessionId());
            return Json(model);
        }
        [HttpGet]
        public IActionResult GetNextRollNo()
        {
            var query = Request.Query;
            var values = new Dictionary<string, string>();
            foreach (var key in query.Keys)
            {
                values[key] = query[key]!;
            }
            string rollNo = _studentService.GetNewStudentRollNo(GetCompanyId(), GetSessionId(), values);
            return Json(new { rollNo });
        }

        [HttpGet]
        public IActionResult GetNextAdmissionNo()
        {
            string admissionNo = _studentService.GetNextSimpleAdmissionNo(GetCompanyId());
            return Json(new { admissionNo });
        }

        [HttpPost]
        /// <summary>
        /// This action is called when the 'Save' button is clicked on the Admission form.
        /// It takes all the student data and sends it to the service to be saved in the database.
        /// </summary>
        public IActionResult SaveStudent([FromBody] StudentAdmissionUpsertRequest req)
        {
            var res = _studentService.UpsertStudentAdmission(req, GetCompanyId(), GetSessionId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetPickupPoints(int routeId)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            var allLinks = _routePickupPointService.GetAllRoutePickupPoints(companyId, sessionId);
            var points = allLinks.Where(l => l.RouteID == routeId && l.IsActive)
                                 .Select(l => new { id = l.PickupPointID, name = l.PickupPointName })
                                 .ToList();
            return Json(points);
        }

        [HttpGet]
        public IActionResult GetHostelRooms(int hostelId)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            var allRooms = _hostelService.GetAllHostelRooms(companyId, sessionId);
            var rooms = allRooms.Where(r => r.HostelID == hostelId && r.IsActive)
                                .Select(r => new { id = r.RoomId, name = r.RoomTitle })
                                .ToList();
            return Json(rooms);
        }

        [HttpGet]
        public IActionResult GetSections(int classId)
        {
            var sections = _sectionService.GetSectionsByClass(classId);
            var result = sections.Where(s => s.IsActive)
                                 .Select(s => new { id = s.SectionID, name = s.SectionName })
                                 .ToList();
            return Json(result);
        }

        [HttpPost]
        public IActionResult DeleteStudent(int id)
        {
            var res = _studentService.BulkDeleteStudents(new List<int> { id }, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        public IActionResult MultiClassStudents()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        public IActionResult DisabledStudents(int? classId, int? sectionId, string? search)
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();

            var model = new StudentListPageViewModel
            {
                Students = _studentService.GetDisabledStudentList(companyId, sessionId, classId, sectionId, search),
                SelectedClassId = classId,
                SelectedSectionId = sectionId,
                SearchTerm = search
            };

            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);
            if (classId.HasValue)
            {
                ViewBag.Sections = _sectionService.GetSectionsByClass(classId.Value);
            }
            else
            {
                ViewBag.Sections = new List<MstSectionViewModel>();
            }

            return View(model);
        }

        public IActionResult BulkDelete()
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);
            return View();
        }

        [HttpPost]
        public IActionResult BulkDeleteStudents([FromBody] List<int> ids)
        {
            var res = _studentService.BulkDeleteStudents(ids, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        public IActionResult ImportStudent()
        {
            var companyId = GetCompanyId();
            var sessionId = GetSessionId();
            ViewBag.Classes = _classService.GetAllClasses(companyId, sessionId);
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ImportStudent(int classId, int sectionId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please select a CSV file." });

            try
            {
                var companyId = GetCompanyId();
                var sessionId = GetSessionId();
                var userId = GetUserId();

                var categories = _studentService.GetAllStudentCategories(companyId, sessionId);
                var houses = _studentService.GetAllStudentHouses(companyId, sessionId);

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
                    var fieldValues = new Dictionary<string, string>();

                    fieldValues["Class"] = classId.ToString();
                    fieldValues["Section"] = sectionId.ToString();

                    for (int i = 0; i < headers.Count && i < values.Count; i++)
                    {
                        string header = headers[i];
                        string value = values[i];
                        string mappedKey = MapCsvHeaderToKey(header);

                        if (!string.IsNullOrEmpty(mappedKey))
                        {
                            if (mappedKey == "Category")
                            {
                                var cat = categories.FirstOrDefault(c => string.Equals(c.StudentCategoryName, value, StringComparison.OrdinalIgnoreCase));
                                value = cat?.StudentCategoryID.ToString() ?? "";
                            }
                            else if (mappedKey == "House")
                            {
                                var house = houses.FirstOrDefault(h => string.Equals(h.StudentHouseName, value, StringComparison.OrdinalIgnoreCase));
                                value = house?.StudentHouseID.ToString() ?? "";
                            }
                            fieldValues[mappedKey] = value;
                        }
                    }

                    var req = new StudentAdmissionUpsertRequest { StudentID = 0, FieldValues = fieldValues };
                    var res = _studentService.UpsertStudentAdmission(req, companyId, sessionId, userId);
                    results.Add(new
                    {
                        admissionNo = fieldValues.GetValueOrDefault("Admission No"),
                        name = (fieldValues.GetValueOrDefault("First Name") + " " + fieldValues.GetValueOrDefault("Last Name")).Trim(),
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

        private string MapCsvHeaderToKey(string header)
        {
            return header switch
            {
                "admission_no" => "Admission No",
                "roll_no" => "Roll No",
                "first_name" => "First Name",
                "middlename" => "Middle Name",
                "last_name" => "Last Name",
                "gender" => "Gender",
                "date_of_birth" => "Date of Birth",
                "category" => "Category",
                "religion" => "Religion",
                "caste" => "Caste",
                "mobile_no" => "Mobile Number",
                "email" => "Email",
                "admission_date" => "Admission Date",
                "blood_group" => "Blood Group",
                "student_house" => "House",
                "height" => "Height",
                "weight" => "Weight",
                "father_name" => "Father Name",
                "father_phone" => "Father Phone",
                "father_occupation" => "Father Occupation",
                "mother_name" => "Mother Name",
                "mother_phone" => "Mother Phone",
                "mother_occupation" => "Mother Occupation",
                "guardian_is" => "If Guardian Is",
                "guardian_name" => "Guardian Name",
                "guardian_relation" => "Guardian Relation",
                "guardian_email" => "Guardian Email",
                "guardian_phone" => "Guardian Phone",
                "guardian_occupation" => "Guardian Occupation",
                "current_address" => "CurrentAddress",
                "permanent_address" => "PermanentAddress",
                _ => ""
            };
        }

        #region Extracted API Endpoints

        [HttpGet]
        public IActionResult GetStudentAttendanceHistory(int id, int year)
        {
            try
            {
                var data = _attendanceService.GetStudentAttendanceHistory(id, year, GetCompanyId());
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllDisableReasons()
        {
            var data = _studentService.GetAllDisableReasons(GetCompanyId(), GetSessionId());
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult UpsertDisableReason([FromBody] StudentDisableReasonUpsertRequest req)
        {
            var res = _studentService.UpsertDisableReason(req, GetCompanyId(), GetSessionId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteDisableReason(int id)
        {
            var res = _studentService.DeleteDisableReason(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetAllStudentHouses()
        {
            var data = _studentService.GetAllStudentHouses(GetCompanyId(), GetSessionId());
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult UpsertStudentHouse([FromBody] StudentHouseUpsertRequest req)
        {
            var res = _studentService.UpsertStudentHouse(req, GetCompanyId(), GetSessionId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteStudentHouse(int id)
        {
            var res = _studentService.DeleteStudentHouse(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetAllStudentCategories()
        {
            var data = _studentService.GetAllStudentCategories(GetCompanyId(), GetSessionId());
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult UpsertStudentCategory([FromBody] StudentCategoryUpsertRequest req)
        {
            var res = _studentService.UpsertStudentCategory(req, GetCompanyId(), GetSessionId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteStudentCategory(int id)
        {
            var res = _studentService.DeleteStudentCategory(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetStudentTimeline(int id)
        {
            try
            {
                var data = _studentService.GetStudentTimeline(id);
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpsertTimeline([FromBody] StudentTimelineUpsertRequest req)
        {
            try
            {
                var result = _studentService.UpsertStudentTimeline(req, GetCompanyId(), GetSessionId(), GetUserId());
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteTimeline(int id)
        {
            try
            {
                var result = _studentService.DeleteStudentTimeline(id, GetUserId());
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadTimelineDoc(int id)
        {
            var (bytes, fileName, contentType) = _studentService.GetStudentTimelineDocument(id);
            if (bytes == null) return NotFound();
            return File(bytes, contentType, fileName);
        }

        [HttpPost]
        public IActionResult ToggleStatus([FromBody] StudentStatusToggleRequest req)
        {
            var res = _studentService.ToggleStudentStatus(req, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetByID(int id)
        {
            try
            {
                var data = _studentService.GetStudentDetails(id, GetCompanyId(), GetSessionId());
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMultiClassStudents(int? classId, int? sectionId, string? searchTerm)
        {
            var data = _studentService.GetMultiClassStudents(GetCompanyId(), GetSessionId(), classId, sectionId, searchTerm);
            return Json(new { success = true, data });
        }

        [HttpPost]
        public IActionResult UpsertMultiClass([FromBody] StudentMultiClassUpsertRequest req)
        {
            var res = _studentService.UpsertStudentMultiClass(req, GetCompanyId(), GetSessionId(), GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public IActionResult DeleteMultiClass(int id)
        {
            var res = _studentService.DeleteStudentMultiClass(id, GetUserId());
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public IActionResult GetClasses()
        {
            var data = _classService.GetAllClasses(GetCompanyId(), GetSessionId());
            return Json(new { success = true, data });
        }

        [HttpGet]
        public IActionResult GetSectionsByClass(int id)
        {
            var data = _sectionService.GetSectionsByClass(id);
            return Json(new { success = true, data });
        }

        #endregion
    }
}
