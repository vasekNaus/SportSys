using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SportSys.Contract.Models;
using SportSys.Contract.Models.hr;
using SportSys.Database.Context;
using SportSys.Database.Models.hr;
using SportSys.Database.Models.identity;

namespace SportSys.Razor.Tests;

public class CoachTptMappingTests
{
    [Fact]
    public void Model_MapsCoachAsTptDerivedUser()
    {
        using var db = CreateDbContext();
        var userType = db.Model.FindEntityType(typeof(User));
        var coachType = db.Model.FindEntityType(typeof(Coach));

        Assert.NotNull(userType);
        Assert.NotNull(coachType);
        Assert.Same(userType, coachType.BaseType);
        Assert.Equal(RelationalAnnotationNames.TptMappingStrategy, userType.GetMappingStrategy());
        Assert.Equal("User", userType.GetTableName());
        Assert.Equal("identity", userType.GetSchema());
        Assert.Equal("Coach", coachType.GetTableName());
        Assert.Equal("hr", coachType.GetSchema());
        Assert.Same(userType.FindProperty(nameof(User.Id)), coachType.FindProperty(nameof(User.Id)));
        Assert.Null(coachType.FindProperty("UserId"));
    }

    [Fact]
    public void Model_KeepsAttendanceReferencesToCoachAndUploadingUser()
    {
        using var db = CreateDbContext();
        var attendanceType = db.Model.FindEntityType(typeof(CoachAttendance));

        Assert.NotNull(attendanceType);

        var coachForeignKey = attendanceType.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == nameof(CoachAttendance.CoachId));
        var userForeignKey = attendanceType.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == nameof(CoachAttendance.UserUploadId));

        Assert.Equal(typeof(Coach), coachForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(typeof(User), userForeignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void CreateScript_UsesNonIdentityCoachPrimaryKeyAndUserForeignKey()
    {
        using var db = CreateDbContext();
        var script = db.Database.GenerateCreateScript();
        var coachTableStart = script.IndexOf("CREATE TABLE [hr].[Coach]", StringComparison.Ordinal);

        Assert.True(coachTableStart >= 0);

        var coachTableEnd = script.IndexOf(");", coachTableStart, StringComparison.Ordinal);
        Assert.True(coachTableEnd > coachTableStart);

        var coachTable = script[coachTableStart..coachTableEnd];
        Assert.Contains("[Id] int NOT NULL", coachTable, StringComparison.Ordinal);
        Assert.DoesNotContain("[Id] int NOT NULL IDENTITY", coachTable, StringComparison.Ordinal);
        Assert.Contains(
            "FOREIGN KEY ([Id]) REFERENCES [identity].[User] ([Id]) ON DELETE CASCADE",
            coachTable,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ContractModels_UseInheritanceAndSingleEntityId()
    {
        Assert.IsAssignableFrom<UserDto>(new CoachDetailDto());
        Assert.IsAssignableFrom<UserListItem>(new CoachListItem());
        Assert.IsAssignableFrom<UserSelectItem>(new CoachSelectItem());
        Assert.IsAssignableFrom<CoachSelectItem>(new TrainingRequirementCoachListItem());

        Assert.NotNull(typeof(CoachDetailDto).GetProperty(nameof(UserDto.Id)));
        Assert.Null(typeof(CoachDetailDto).GetProperty("CoachId"));
        Assert.NotNull(typeof(CoachAttendanceUploadDto).GetProperty(
            nameof(CoachAttendanceUploadDto.CoachId)));
    }

    private static SportSysDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysTptModel;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;

        return new SportSysDbContext(options);
    }
}
