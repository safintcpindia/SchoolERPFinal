using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SchoolERP.Net.Models;
using SchoolERP.Net.Models.Common;

namespace SchoolERP.Net.Services.Clients
{
    public class StudentInformationClientService : BaseApiClient, IStudentInformationClientService
    {
        public StudentInformationClientService(HttpClient httpClient) : base(httpClient) { }

        public Task<ApiResponse<List<StudentListViewModel>>> GetStudentListAsync(int? classId = null, int? sectionId = null, string? searchTerm = null)
        {
            var url = $"api/StudentInformationApi/GetStudentList?classId={classId}&sectionId={sectionId}&searchTerm={searchTerm}";
            return GetAsync<List<StudentListViewModel>>(url);
        }

        public Task<ApiResponse<StudentDetailsViewModel>> GetStudentByIDAsync(int id)
        {
            return GetAsync<StudentDetailsViewModel>($"api/StudentInformationApi/GetByID/{id}");
        }
    }
}
