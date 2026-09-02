using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class GetStudentProfileServiceTests
{
    private readonly FakeStudentRepository _students = new();

    private GetStudentProfileService CreateService() => new(_students);

    [Fact]
    public async Task Get_ExistingStudent_ReturnsPublicSafeProfile()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        student.SetPasswordHash("hashed:secret");
        _students.Seed(student);

        var result = await CreateService().GetAsync(student.Id);

        result.StudentId.Should().Be(student.Id);
        result.Name.Should().Be("Sara");
        result.Email.Should().Be("sara@example.com");
    }

    [Fact]
    public async Task Get_UnknownStudent_ThrowsNotFound()
    {
        var act = () => CreateService().GetAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}