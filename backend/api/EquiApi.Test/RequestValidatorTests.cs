using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.Test.Util;
using FluentValidation.TestHelper;

namespace EquiApi.Test;

public sealed class RequestValidatorTests
{
    private static readonly AddressRequest validAddress = new(null, "Steyr", "4400");

    private readonly IClock _clock = TestData.CreateClock();

    [Fact]
    public void AddPersonRequest_ValidEquestrian_HasNoErrors()
    {
        var result = new AddPersonRequest.Validator(_clock).TestValidate(CreateAddPersonRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AddPersonRequest_BirthDateTodayOrInFuture_HasError(int daysFromToday)
    {
        var request = CreateAddPersonRequest() with { DateOfBirth = TestData.Today.PlusDays(daysFromToday) };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddPersonRequest_HeightNotPositive_HasError(decimal height)
    {
        var request = CreateAddPersonRequest() with { Height = height };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Height);
    }

    [Fact]
    public void AddPersonRequest_InvalidEmail_HasError()
    {
        var request = CreateAddPersonRequest() with { Email = "max.example.com" };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void AddPersonRequest_EquestrianWithWebsiteAndDescription_HasErrors()
    {
        var request = CreateAddPersonRequest() with
        {
            WebsiteLink = "https://example.com",
            Description = "Description"
        };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.WebsiteLink);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void AddPersonRequest_SaddlerWithWebsiteAndDescription_HasNoErrors()
    {
        var request = CreateAddPersonRequest() with
        {
            Role = RoleName.Saddler,
            WebsiteLink = "https://example.com",
            Description = "Description",
            Address = validAddress with { AddressName = "Lenaustraße 18" }
        };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddPersonRequest_SaddlerWithoutStreet_HasError(string? street)
    {
        var request = CreateAddPersonRequest() with
        {
            Role = RoleName.Saddler,
            Address = validAddress with { AddressName = street }
        };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor("Address.AddressName");
    }

    [Fact]
    public void AddPersonRequest_EquestrianWithoutStreet_HasNoErrors()
    {
        var request = CreateAddPersonRequest() with { Address = validAddress with { AddressName = null } };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddPersonRequest_AddressWithoutCity_HasError()
    {
        var request = CreateAddPersonRequest() with { Address = validAddress with { CityName = "" } };

        var result = new AddPersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor("Address.CityName");
    }

    [Fact]
    public void UpdatePersonRequest_AllValuesNull_HasNoErrors()
    {
        var request = new UpdatePersonRequest(null, null, null, null, null, null, null, null, null, null);

        var result = new UpdatePersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePersonRequest_OnlySomeValuesSet_HasNoErrors()
    {
        var request = new UpdatePersonRequest("Moritz", null, 185M, null, null, null, null, null, null, null);

        var result = new UpdatePersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePersonRequest_InvalidValues_HasErrors()
    {
        var request = new UpdatePersonRequest("", null, 0M, null, TestData.Today, null, null, null,
                                              validAddress with { PLZ = "" }, null);

        var result = new UpdatePersonRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.Height);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        result.ShouldHaveValidationErrorFor("Address.PLZ");
    }

    [Fact]
    public void AddHorseRequest_ValidData_HasNoErrors()
    {
        var result = new AddHorseRequest.Validator(_clock).TestValidate(CreateAddHorseRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddHorseRequest_BirthDateInFuture_HasError()
    {
        var request = CreateAddHorseRequest() with { DateOfBirth = TestData.Today.PlusDays(1) };

        var result = new AddHorseRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void AddHorseRequest_NoBreeds_HasError()
    {
        var request = CreateAddHorseRequest() with { BreedIds = [] };

        var result = new AddHorseRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BreedIds);
    }

    [Fact]
    public void AddHorseRequest_InvalidBreedId_HasError()
    {
        var request = CreateAddHorseRequest() with { BreedIds = [1, 0] };

        var result = new AddHorseRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor("BreedIds[1]");
    }

    [Fact]
    public void AddDeviceRequest_InvalidValues_HasErrors()
    {
        var request = new AddDeviceRequest("", 0, -1);

        var result = new AddDeviceRequest.Validator().TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
        result.ShouldHaveValidationErrorFor(x => x.OwnerId);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void AddMeasurementRequest_ValidData_HasNoErrors()
    {
        var request = new AddMeasurementRequest("Schritt", "Links", null);

        var result = new AddMeasurementRequest.Validator().TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddMeasurementRequest_InvalidValues_HasErrors()
    {
        var request = new AddMeasurementRequest("", "", new string('x', 501));

        var result = new AddMeasurementRequest.Validator().TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Pace);
        result.ShouldHaveValidationErrorFor(x => x.Hand);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void AddMeasurementGroupRequest_DateTodayOrInPast_HasNoErrors(int daysFromToday)
    {
        var request = CreateAddMeasurementGroupRequest() with { Date = TestData.Today.PlusDays(daysFromToday) };

        var result = new AddMeasurementGroupRequest.Validator(_clock).TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddMeasurementGroupRequest_DateInFuture_HasError()
    {
        var request = CreateAddMeasurementGroupRequest() with { Date = TestData.Today.PlusDays(1) };

        var result = new AddMeasurementGroupRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void AddMeasurementGroupRequest_InvalidValues_HasErrors()
    {
        var request = new AddMeasurementGroupRequest("", TestData.Today, new string('x', 501), 0, -1, 0, "");

        var result = new AddMeasurementGroupRequest.Validator(_clock).TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
        result.ShouldHaveValidationErrorFor(x => x.PersonId);
        result.ShouldHaveValidationErrorFor(x => x.HorseId);
        result.ShouldHaveValidationErrorFor(x => x.SaddleId);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    private static AddPersonRequest CreateAddPersonRequest() =>
        new("Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1), "max@example.com", null, null, validAddress,
            RoleName.Equestrian);

    private static AddMeasurementGroupRequest CreateAddMeasurementGroupRequest() =>
        new("Dressurtraining", TestData.Today, null, 1, 1, 1, "EP0701");

    private static AddHorseRequest CreateAddHorseRequest() =>
        new("Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, validAddress, [1], 1);
}