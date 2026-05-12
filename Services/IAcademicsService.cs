using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolERP.Net.Models;

namespace SchoolERP.Net.Services
{
    public interface IAcademicsService
    {
        List<TimeTableViewModel> GetTimeTableByClass(int companyId, int sessionId, int classId, int sectionId);
        List<TimeTableViewModel> GetTimeTableByStaff(int companyId, int sessionId, int staffId);
        (bool success, string message) UpsertTimeTable(TimeTableUpsertRequest req, int companyId, int sessionId, int userId);
        (bool success, string message) DeleteTimeTableSlot(int id, int userId);

        List<ClassTeacherViewModel> GetAllClassTeachers(int companyId, int sessionId);
        (bool success, string message) UpsertClassTeacher(ClassTeacherUpsertRequest req, int companyId, int sessionId, int userId);
        (bool success, string message) DeleteClassTeacher(int id, int userId);

        List<StudentPromotionViewModel> GetStudentsForPromotion(int companyId, int sessionId, int classId, int sectionId);
        (bool success, string message) PromoteStudents(PromotionRequest req, int companyId, int userId);
    }
}
