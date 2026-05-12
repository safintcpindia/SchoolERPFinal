using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolERP.Net.Models;
using SchoolERP.Net.Models.Common;

namespace SchoolERP.Net.Services.Clients
{
    public interface IStudentInformationClientService
    {
        Task<ApiResponse<List<StudentListViewModel>>> GetStudentListAsync(int? classId = null, int? sectionId = null, string? searchTerm = null);
        Task<ApiResponse<StudentDetailsViewModel>> GetStudentByIDAsync(int id);
    }
}
