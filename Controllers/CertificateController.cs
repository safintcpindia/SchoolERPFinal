using Microsoft.AspNetCore.Mvc;
using SchoolERP.Net.Models;
using SchoolERP.Net.Services.Clients;
using System.Threading.Tasks;

using SchoolERP.Net.Services;
using System.Collections.Generic;

namespace SchoolERP.Net.Controllers
{
    public class CertificateController : Controller
    {
        private readonly IStudentCertificateClientService _certificateClient;
        private readonly IStudentIDCardClientService _idCardClient;
        private readonly IStaffIDCardClientService _staffIdCardClient;
        private readonly IClassClientService _classClient;
        private readonly ISectionClientService _sectionClient;
        private readonly IStudentInformationClientService _studentInfoClient;
        private readonly IHumanResourceClientService _hrClient;
        private readonly IUserMenuPermissionService _menuPerm;

        public CertificateController(
            IStudentCertificateClientService certificateClient, 
            IStudentIDCardClientService idCardClient,
            IStaffIDCardClientService staffIdCardClient,
            IClassClientService classClient,
            ISectionClientService sectionClient,
            IStudentInformationClientService studentInfoClient,
            IHumanResourceClientService hrClient,
            IUserMenuPermissionService menuPerm)
        {
            _certificateClient = certificateClient;
            _idCardClient = idCardClient;
            _staffIdCardClient = staffIdCardClient;
            _classClient = classClient;
            _sectionClient = sectionClient;
            _studentInfoClient = studentInfoClient;
            _hrClient = hrClient;
            _menuPerm = menuPerm;
        }

        public async Task<IActionResult> StudentCertificate()
        {
            var model = new StudentCertificatePageViewModel();
            var resp = await _certificateClient.GetAll();
            if (resp.Success)
            {
                model.Certificates = resp.Data;
            }
            return View(model);
        }

        public IActionResult GenerateCertificate()
        {
            return View();
        }

        public async Task<IActionResult> StudentIdCard()
        {
            var model = new StudentIDCardPageViewModel();
            var resp = await _idCardClient.GetAll();
            if (resp.Success)
            {
                model.IDCards = resp.Data;
            }
            return View(model);
        }

        public IActionResult GenerateStudentIdCard()
        {
            return View();
        }

        public async Task<IActionResult> StaffIdCard()
        {
            var model = new StaffIDCardPageViewModel();
            var resp = await _staffIdCardClient.GetAll();
            if (resp.Success)
            {
                model.IDCards = resp.Data;
            }
            return View(model);
        }

        public IActionResult GenerateStaffIdCard()
        {
            return View();
        }

        #region Certificate Module API Proxy Endpoints

        // --- Classes & Sections ---
        [HttpGet]
        public async Task<IActionResult> GetAllClasses()
        {
            var res = await _classClient.GetAllAsync();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetSectionsByClass(int classId)
        {
            var res = await _sectionClient.GetByClassAsync(classId);
            return Json(res);
        }

        // --- Student Information ---
        [HttpGet]
        public async Task<IActionResult> GetStudentList(int? classId = null, int? sectionId = null, string? searchTerm = null)
        {
            var res = await _studentInfoClient.GetStudentListAsync(classId, sectionId, searchTerm);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentByID(int id)
        {
            var res = await _studentInfoClient.GetStudentByIDAsync(id);
            return Json(res);
        }

        // --- Human Resources ---
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var res = await _hrClient.GetAllDepartmentsAsync();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDesignations()
        {
            var res = await _hrClient.GetAllDesignationsAsync();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStaff()
        {
            var res = await _hrClient.GetAllStaffAsync();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffByID(int id)
        {
            var res = await _hrClient.GetStaffByIDAsync(id);
            return Json(res);
        }

        // --- Student ID Cards ---
        [HttpGet]
        public async Task<IActionResult> GetAllStudentIDCards()
        {
            var res = await _idCardClient.GetAll();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentIDCardByID(int id)
        {
            var res = await _idCardClient.GetByID(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertStudentIDCard([FromBody] StudentIDCardUpsertRequest request)
        {
            var isCreate = request.IDCardID <= 0;
            if (isCreate && !_menuPerm.Has(User, "/Certificate/StudentIdCard", "Add"))
                return Json(new { success = false, message = "You do not have permission to add student ID cards." });
            if (!isCreate && !_menuPerm.Has(User, "/Certificate/StudentIdCard", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit student ID cards." });

            var res = await _idCardClient.Upsert(request);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudentIDCard(int id)
        {
            if (!_menuPerm.Has(User, "/Certificate/StudentIdCard", "Delete"))
                return Json(new { success = false, message = "You do not have permission to delete student ID cards." });

            var res = await _idCardClient.Delete(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStudentIDCardStatus(int id, bool isActive)
        {
            if (!_menuPerm.Has(User, "/Certificate/StudentIdCard", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit student ID cards." });

            var res = await _idCardClient.ToggleStatus(id, isActive);
            return Json(res);
        }

        // --- Staff ID Cards ---
        [HttpGet]
        public async Task<IActionResult> GetAllStaffIDCards()
        {
            var res = await _staffIdCardClient.GetAll();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffIDCardByID(int id)
        {
            var res = await _staffIdCardClient.GetByID(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertStaffIDCard([FromBody] StaffIDCardUpsertRequest request)
        {
            var isCreate = request.IDCardID <= 0;
            if (isCreate && !_menuPerm.Has(User, "/Certificate/StaffIdCard", "Add"))
                return Json(new { success = false, message = "You do not have permission to add staff ID cards." });
            if (!isCreate && !_menuPerm.Has(User, "/Certificate/StaffIdCard", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit staff ID cards." });

            var res = await _staffIdCardClient.Upsert(request);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStaffIDCard(int id)
        {
            if (!_menuPerm.Has(User, "/Certificate/StaffIdCard", "Delete"))
                return Json(new { success = false, message = "You do not have permission to delete staff ID cards." });

            var res = await _staffIdCardClient.Delete(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStaffIDCardStatus(int id, bool isActive)
        {
            if (!_menuPerm.Has(User, "/Certificate/StaffIdCard", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit staff ID cards." });

            var res = await _staffIdCardClient.ToggleStatus(id, isActive);
            return Json(res);
        }

        // --- Student Certificates ---
        [HttpGet]
        public async Task<IActionResult> GetAllStudentCertificates()
        {
            var res = await _certificateClient.GetAll();
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentCertificateByID(int id)
        {
            var res = await _certificateClient.GetByID(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertStudentCertificate([FromBody] StudentCertificateUpsertRequest request)
        {
            var isCreate = request.CertificateID <= 0;
            if (isCreate && !_menuPerm.Has(User, "/Certificate/StudentCertificate", "Add"))
                return Json(new { success = false, message = "You do not have permission to add student certificates." });
            if (!isCreate && !_menuPerm.Has(User, "/Certificate/StudentCertificate", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit student certificates." });

            var res = await _certificateClient.Upsert(request);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudentCertificate(int id)
        {
            if (!_menuPerm.Has(User, "/Certificate/StudentCertificate", "Delete"))
                return Json(new { success = false, message = "You do not have permission to delete student certificates." });

            var res = await _certificateClient.Delete(id);
            return Json(res);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStudentCertificateStatus(int id, bool isActive)
        {
            if (!_menuPerm.Has(User, "/Certificate/StudentCertificate", "Edit"))
                return Json(new { success = false, message = "You do not have permission to edit student certificates." });

            var res = await _certificateClient.ToggleStatus(id, isActive);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateStudentCertificate(int studentId, int certificateId)
        {
            var res = await _certificateClient.Generate(studentId, certificateId);
            return Json(res);
        }

        #endregion
    }
}
