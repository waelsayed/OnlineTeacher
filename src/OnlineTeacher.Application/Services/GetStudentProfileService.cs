using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads a student's public-safe profile by central identity. The student exists centrally
/// and is never tenant-scoped. The password hash is never returned.
/// </summary>
public sealed class GetStudentProfileService
{
    private readonly IStudentRepository _students;

    public GetStudentProfileService(IStudentRepository students)
    {
        _students = students;
    }

    public async Task<StudentProfile> GetAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        return new StudentProfile(student.Id, student.Name, student.Email.Value);
    }
}