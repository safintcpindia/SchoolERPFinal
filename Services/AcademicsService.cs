using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SchoolERP.Net.Data;
using SchoolERP.Net.Helpers;
using SchoolERP.Net.Models;

namespace SchoolERP.Net.Services
{
    /// <summary>
    /// This service manages the academic schedule and assignments.
    /// It handles the school timetable (when and where classes happen) and 
    /// keeps track of which teacher is assigned to which class.
    /// </summary>
    public class AcademicsService : IAcademicsService
    {
        private readonly SqlHelper _db;

        public AcademicsService(SqlHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets the school timetable for a specific class and section.
        /// This shows which subjects are taught at what time for the students.
        /// </summary>
        public List<TimeTableViewModel> GetTimeTableByClass(int companyId, int sessionId, int classId, int sectionId)
        {
            var list = new List<TimeTableViewModel>();
            var p = new[] {
                new SqlParameter("@CompanyID", companyId),
                new SqlParameter("@SessionID", sessionId),
                new SqlParameter("@ClassID",   classId),
                new SqlParameter("@SectionID", sectionId)
            };
            var dt = _db.ExecuteQuery("SP_ACD_TIMETABLE_GETBYCLASS", p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapRowToViewModel(row));
            }
            return list;
        }

        /// <summary>
        /// Gets the timetable for a specific teacher.
        /// This helps teachers see their daily schedule and assigned rooms.
        /// </summary>
        public List<TimeTableViewModel> GetTimeTableByStaff(int companyId, int sessionId, int staffId)
        {
            var list = new List<TimeTableViewModel>();
            var p = new[] {
                new SqlParameter("@CompanyID", companyId),
                new SqlParameter("@SessionID", sessionId),
                new SqlParameter("@StaffID",   staffId)
            };
            var dt = _db.ExecuteQuery("SP_ACD_TIMETABLE_GETBYSTAFF", p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapRowToViewModel(row));
            }
            return list;
        }

        /// <summary>
        /// Adds or updates a slot in the timetable (e.g., adding a Math class on Monday at 9 AM).
        /// </summary>
        public (bool success, string message) UpsertTimeTable(TimeTableUpsertRequest req, int companyId, int sessionId, int userId)
        {
            try
            {
                var p = new[] {
                    new SqlParameter("@TimeTableID", req.TimeTableID),
                    new SqlParameter("@CompanyID",   companyId),
                    new SqlParameter("@SessionID",   sessionId),
                    new SqlParameter("@ClassID",     req.ClassID),
                    new SqlParameter("@SectionID",   req.SectionID),
                    new SqlParameter("@SubjectID",   req.SubjectID),
                    new SqlParameter("@StaffID",     req.StaffID),
                    new SqlParameter("@Day",         req.Day),
                    new SqlParameter("@StartTime",   req.StartTime),
                    new SqlParameter("@EndTime",     req.EndTime),
                    new SqlParameter("@RoomNo",      (object?)req.RoomNo ?? DBNull.Value),
                    new SqlParameter("@UserId",      userId)
                };
                var dt = _db.ExecuteQuery("SP_ACD_TIMETABLE_UPSERT", p);
                return (Convert.ToInt32(dt.Rows[0]["Result"]) > 0, dt.Rows[0]["Message"].ToString()!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>
        /// Removes a specific class slot from the timetable.
        /// </summary>
        public (bool success, string message) DeleteTimeTableSlot(int id, int userId)
        {
            try
            {
                var p = new[] { new SqlParameter("@TimeTableID", id), new SqlParameter("@UserId", userId) };
                var dt = _db.ExecuteQuery("SP_ACD_TIMETABLE_DELETE", p);
                return (Convert.ToInt32(dt.Rows[0]["Result"]) > 0, dt.Rows[0]["Message"].ToString()!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>
        /// Retrieves a list of all class teachers assigned to different classes.
        /// </summary>
        public List<ClassTeacherViewModel> GetAllClassTeachers(int companyId, int sessionId)
        {
            var list = new List<ClassTeacherViewModel>();
            var p = new[] {
                new SqlParameter("@CompanyID", companyId),
                new SqlParameter("@SessionID", sessionId)
            };
            var dt = _db.ExecuteQuery("sp_Academics_ClassTeacher_GetAll", p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ClassTeacherViewModel
                {
                    ClassTeacherID = Convert.ToInt32(row["ClassTeacherID"]),
                    CompanyID = Convert.ToInt32(row["CompanyID"]),
                    SessionID = Convert.ToInt32(row["SessionID"]),
                    ClassID = Convert.ToInt32(row["ClassID"]),
                    ClassName = row["ClassName"].ToString()!,
                    SectionID = Convert.ToInt32(row["SectionID"]),
                    SectionName = row["SectionName"].ToString()!,
                    StaffID = Convert.ToInt32(row["StaffID"]),
                    StaffName = row["StaffName"].ToString()!,
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }
            return list;
        }

        public (bool success, string message) UpsertClassTeacher(ClassTeacherUpsertRequest req, int companyId, int sessionId, int userId)
        {
            try
            {
                var p = new[] {
                    new SqlParameter("@CompanyID", companyId),
                    new SqlParameter("@SessionID", sessionId),
                    new SqlParameter("@ClassID",   req.ClassID),
                    new SqlParameter("@SectionID", req.SectionID),
                    new SqlParameter("@StaffIDs",  string.Join(",", req.StaffIDs)),
                    new SqlParameter("@UserId",    userId)
                };
                var dt = _db.ExecuteQuery("sp_Academics_ClassTeacher_Upsert", p);
                return (Convert.ToInt32(dt.Rows[0]["Result"]) > 0, dt.Rows[0]["Message"].ToString()!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public (bool success, string message) DeleteClassTeacher(int id, int userId)
        {
            try
            {
                var p = new[] { new SqlParameter("@ClassTeacherID", id), new SqlParameter("@UserId", userId) };
                var dt = _db.ExecuteQuery("sp_Academics_ClassTeacher_Delete", p);
                return (Convert.ToInt32(dt.Rows[0]["Result"]) > 0, dt.Rows[0]["Message"].ToString()!);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public List<StudentPromotionViewModel> GetStudentsForPromotion(int companyId, int sessionId, int classId, int sectionId)
        {
            var list = new List<StudentPromotionViewModel>();
            var p = new[] {
                new SqlParameter("@CompanyID", companyId),
                new SqlParameter("@SessionID", sessionId),
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@SectionID", sectionId)
            };
            var dt = _db.ExecuteQuery("sp_Academics_Students_GetForPromotion", p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new StudentPromotionViewModel
                {
                    StudentID = Convert.ToInt32(row["StudentID"]),
                    AdmissionNo = row["AdmissionNo"].ToString()!,
                    StudentName = row["StudentName"].ToString()!,
                    FatherName = row["FatherName"].ToString()!,
                    DOB = row["DOB"] != DBNull.Value ? Convert.ToDateTime(row["DOB"]) : (DateTime?)null
                });
            }
            return list;
        }

        public (bool success, string message) PromoteStudents(PromotionRequest req, int companyId, int userId)
        {
            int successCount = 0;
            string lastError = "";
            foreach (var student in req.Students)
            {
                try
                {
                    var p = new[] {
                        new SqlParameter("@StudentID", student.StudentID),
                        new SqlParameter("@CompanyID", companyId),
                        new SqlParameter("@NextSessionID", req.NextSessionID),
                        new SqlParameter("@NextClassID", req.NextClassID),
                        new SqlParameter("@NextSectionID", req.NextSectionID),
                        new SqlParameter("@Result", student.Result),
                        new SqlParameter("@NextStatus", student.NextStatus),
                        new SqlParameter("@UserId", userId)
                    };
                    var dt = _db.ExecuteQuery("sp_Academics_Student_Promote_Single", p);
                    if (Convert.ToInt32(dt.Rows[0]["Result"]) > 0) successCount++;
                    else lastError = dt.Rows[0]["Message"].ToString()!;
                }
                catch (Exception ex) { lastError = ex.Message; }
            }

            if (successCount == req.Students.Count)
                return (true, $"{successCount} students promoted successfully.");
            else if (successCount > 0)
                return (true, $"{successCount} students promoted. Last error: {lastError}");
            else
                return (false, "Failed to promote students: " + lastError);
        }

        private TimeTableViewModel MapRowToViewModel(DataRow row)
        {
            return new TimeTableViewModel
            {
                TimeTableID = Convert.ToInt32(row["TimeTableID"]),
                CompanyID = Convert.ToInt32(row["CompanyID"]),
                SessionID = Convert.ToInt32(row["SessionID"]),
                ClassID = Convert.ToInt32(row["ClassID"]),
                ClassName = row["ClassName"].ToString()!,
                SectionID = Convert.ToInt32(row["SectionID"]),
                SectionName = row["SectionName"].ToString()!,
                SubjectID = Convert.ToInt32(row["SubjectID"]),
                SubjectName = row["SubjectName"].ToString()!,
                StaffID = Convert.ToInt32(row["StaffID"]),
                StaffName = row["StaffName"].ToString()!,
                Day = row["Day"].ToString()!,
                StartTime = row["StartTime"].ToString()!,
                EndTime = row["EndTime"].ToString()!,
                RoomNo = row["RoomNo"]?.ToString(),
                IsActive = Convert.ToBoolean(row["IsActive"])
            };
        }
    }
}
