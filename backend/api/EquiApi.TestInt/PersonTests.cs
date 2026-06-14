using System.Net;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;

namespace EquiApi.TestInt;

public sealed class PersonIntegrationTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private const string BaseUrl = "api/persons";

    // funktioniert noch nicht
    [Fact]
    public async ValueTask CreatePerson_Success()
    {
        var request = new DataTransfer.AddPersonRequest(
            "Max",
            "Mustermann",
            180M,
            75M,
            new LocalDate(1995, 5, 20),
            "max.success@reiter.at",
            "https://reiter.at",
            "Ein valider Test-Reiter",
            new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            },
            new AccountRole { Name = RoleName.Equestrian }
        );

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    // funktioniert noch nicht
    [Fact]
    public async ValueTask CreatePerson_EmailConflict_ReturnsConflict()
    {
        const string DuplicateEmail = "duplicate@reiter.at";
        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(new Person
            {
                FirstName = "Existing",
                LastName = "User",
                Height = 175M,
                Weight = 70M,
                DateOfBirth = new LocalDate(1990, 1, 1),
                Email = DuplicateEmail,
    
                Address = new Address
                {
                    PLZ = "4050",
                    CityName = "Traun"
                    
                },
                Roles = new List<PersonRoleAssignment>
                {
                    new PersonRoleAssignment
                    {
                        Role = new AccountRole
                        {
                            Name = RoleName.Equestrian
                        }
                    }
                }
            });
            await ctx.SaveChangesAsync();
        });

        var request = new DataTransfer.AddPersonRequest(
                                                        "Max",
                                                        "Mustermann",
                                                        180M,
                                                        75M,
                                                        new LocalDate(1995, 5, 20),
                                                        DuplicateEmail,
                                                        null,
                                                        null,
                                                        new Address
                                                        {
                                                            PLZ = "4050",
                                                            CityName = "Traun"
                    
                                                        },
                                                        new AccountRole { Name = RoleName.Equestrian }
                                                       );

        
        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CreatePerson_InvalidRequest_ValidationFails_ReturnsBadRequest()
    {
        var request = new DataTransfer.AddPersonRequest(
            string.Empty,
            "Mustermann",
            180M,
            75M,
            new LocalDate(1995, 5, 20),
            "invalid.fields@reiter.at",
            null,
            null,
            new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            },
            new AccountRole { Name = RoleName.Equestrian }
        );

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreatePerson_InvalidDataFromService_ReturnsBadRequest()
    {
        var request = new DataTransfer.AddPersonRequest(
            "Zukunfts",
            "Reiter",
            180M,
            75M,
            LocalDate.FromDateTime(DateTime.Today.AddDays(1)),
            "future@reiter.at",
            null,
            null,
            new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            },
            new AccountRole { Name = RoleName.Equestrian }
        );

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}