using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using NSubstitute;

namespace EquiApi.Test;

public sealed class PersonTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<PersonService> _loggerMock = Substitute.For<ILogger<PersonService>>();
    private readonly PersonService _sut;
    
    public PersonTests()
    {
        _sut = new PersonService(_uowMock, _dateTimeProviderMock, _loggerMock);
        
        _dateTimeProviderMock.GetCurrentDate().Returns(new LocalDate(2026, 1, 1));
    }

    [Fact]
    public async Task AddPerson_WithInvalidPhysicalData_ReturnsInvalidData()
    {
        var invalidHeight = 0M; 

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", invalidHeight, 75M, new LocalDate(1995, 5, 20),
            "max@reiter.at", null, null, new Address
            {
                PLZ = "4050",
                CityName = "Traun"
            }, new AccountRole { Name = RoleName.Equestrian }
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_WithBirthDateInFuture_ReturnsInvalidData()
    {
        var futureDate = new LocalDate(2026, 5, 20); 

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, futureDate,
            "max@reiter.at", null, null, new Address
                {
                    PLZ = "4050",
                    CityName = "Traun"
                    
                }, new AccountRole { Name = RoleName.Equestrian }
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_WithExistingEmail_ReturnsConflict()
    {
        const string existingEmail = "duplicate@reiter.at";
        _uowMock.PersonRepository.PersonWithEmailExistsAsync(existingEmail).Returns(true);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            existingEmail, null, null, new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            }, new AccountRole { Name = RoleName.Equestrian }
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_EquestrianWithWebsite_ReturnsConflict()
    {
        var website = "https://my-stable.at"; 

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            "max@reiter.at", website, null, new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            }, new AccountRole { Name = RoleName.Equestrian }
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_ValidData_SavesToDatabaseAndReturnsSuccess()
    {
        _uowMock.PersonRepository.PersonWithEmailExistsAsync(Arg.Any<string>()).Returns(false);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            "max.unique@reiter.at", null, null, new Address
            {
                PLZ = "4050",
                CityName = "Traun"
                    
            }, new AccountRole { Id = 1, Name = RoleName.Equestrian }
        );

        result.IsT0.Should().BeTrue();
        
        _uowMock.PersonRepository.Received(1).AddPerson(Arg.Any<Person>());
        await _uowMock.Received(1).SaveChangesAsync();
    }
}