using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

public sealed class HorseTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask GetHorseById_ExistingHorse_ReturnsHorseWithBreedsAndAddress()
    {
        var address = TestData.CreateAddress("Stallweg 3", "4050", "Traun");
        var horse = TestData.CreateHorse("Amigo", address,
                                         [TestData.CreateBreed("Haflinger"), TestData.CreateBreed("Noriker")]);
        await SeedAsync(horse);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<HorseDto>(response);
        content.Id.Should().Be(horse.Id);
        content.Name.Should().Be("Amigo");
        content.DateOfBirth.Should().Be(horse.DateOfBirth);
        content.Weight.Should().Be(horse.Weight);
        content.Height.Should().Be(horse.Height);
        content.Gender.Should().Be(HorseGender.Male);
        content.BreedNames.Should().BeEquivalentTo(["Haflinger", "Noriker"]);
        content.Address.Should().Be(new AddressDto(address.Id, "Stallweg 3", "Traun", "4050"));
    }

    [Fact]
    public async ValueTask GetHorseById_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{TestData.NonExistingId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHorseById_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_ValidData_ReturnsCreatedHorse()
    {
        var owner = TestData.CreatePerson();
        var breed = TestData.CreateBreed();
        await SeedAsync(owner, breed);
        var request = TestData.CreateAddHorseRequest(owner.Id, [breed.Id]);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<HorseDto>(response);
        content.Name.Should().Be(request.Name);
        content.Gender.Should().Be(request.Gender);
        content.BreedNames.Should().Equal(breed.Name);

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedHorse = await ReadContentAsync<HorseDto>(getResponse);
        storedHorse.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async ValueTask CreateHorse_ValidData_HorseIsOwnedByOwner()
    {
        var owner = TestData.CreatePerson();
        var breed = TestData.CreateBreed();
        await SeedAsync(owner, breed);
        var request = TestData.CreateAddHorseRequest(owner.Id, [breed.Id]);

        var createResponse = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions,
                                                             TestCancellationToken);
        var createdHorse = await ReadContentAsync<HorseDto>(createResponse);

        var response = await ApiClient.GetAsync($"api/persons/{owner.Id}/horses", TestCancellationToken);
        var content = await ReadContentAsync<HorseListResponse>(response);
        content.Horses.Should().ContainSingle().Which.Id.Should().Be(createdHorse.Id);

        var ridersResponse = await ApiClient.GetAsync($"api/horses/{createdHorse.Id}/riders", TestCancellationToken);
        var riders = await ReadContentAsync<PersonListResponse>(ridersResponse);
        riders.Persons.Should().ContainSingle().Which.Id.Should().Be(owner.Id);
    }

    [Fact]
    public async ValueTask CreateHorse_OwnerNotFound_ReturnsNotFound()
    {
        var breed = TestData.CreateBreed();
        await SeedAsync(breed);
        var request = TestData.CreateAddHorseRequest(TestData.NonExistingId, [breed.Id]);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateHorse_BreedNotFound_ReturnsNotFound()
    {
        var owner = TestData.CreatePerson();
        var breed = TestData.CreateBreed();
        await SeedAsync(owner, breed);
        var request = TestData.CreateAddHorseRequest(owner.Id, [breed.Id, TestData.NonExistingId]);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateHorse_InvalidHeight_ReturnsBadRequest()
    {
        var request = TestData.CreateAddHorseRequest(1, [1], height: 0M);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_BirthDateInFuture_ReturnsBadRequest()
    {
        var request = TestData.CreateAddHorseRequest(1, [1], dateOfBirth: WebAppFactory.CurrentDate.PlusDays(1));

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_NoBreeds_ReturnsBadRequest()
    {
        var request = TestData.CreateAddHorseRequest(1, []);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetSaddles_HorseWithSaddles_ReturnsSaddlesWithCategory()
    {
        var horse = TestData.CreateHorse();
        var saddle = TestData.CreateSaddle(horse, "Dressursattel", "Dressage");
        await SeedAsync(saddle);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/saddles", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<SaddleListResponse>(response);
        content.Saddles.Should().ContainSingle().Which.Should()
               .Be(new SaddleDto(saddle.Id, "Dressursattel", horse.Id, saddle.CategoryId, "Dressage"));
    }

    [Fact]
    public async ValueTask GetSaddles_HorseWithoutSaddles_ReturnsEmptyList()
    {
        var horse = TestData.CreateHorse();
        await SeedAsync(horse);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/saddles", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<SaddleListResponse>(response);
        content.Saddles.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetSaddles_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{TestData.NonExistingId}/saddles",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetSaddles_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1/saddles", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetRiders_HorseWithRiders_ReturnsOwnerFirstAndNoHiddenRiders()
    {
        var horse = TestData.CreateHorse();
        var owner = TestData.CreatePerson("Otto", "Zeller");
        var rider = TestData.CreatePerson("Rita", "Auer");
        var hiddenRider = TestData.CreatePerson("Hans", "Berger");
        await SeedAsync(TestData.CreatePersonHorse(rider, horse),
                        TestData.CreatePersonHorse(owner, horse, isOwner: true),
                        TestData.CreatePersonHorse(hiddenRider, horse, isHidden: true));

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/riders", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonListResponse>(response);
        content.Persons.Select(p => p.Id).Should().Equal(owner.Id, rider.Id);
    }

    [Fact]
    public async ValueTask GetRiders_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{TestData.NonExistingId}/riders",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHiddenRiders_HorseWithRiders_ReturnsOnlyHiddenRiders()
    {
        var horse = TestData.CreateHorse();
        var owner = TestData.CreatePerson("Otto");
        var hiddenRider = TestData.CreatePerson("Hans");
        await SeedAsync(TestData.CreatePersonHorse(owner, horse, isOwner: true),
                        TestData.CreatePersonHorse(hiddenRider, horse, isHidden: true));

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/riders/hidden", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonListResponse>(response);
        content.Persons.Should().ContainSingle().Which.Id.Should().Be(hiddenRider.Id);
    }

    [Fact]
    public async ValueTask GetHiddenRiders_NoHiddenRiders_ReturnsEmptyList()
    {
        var horse = TestData.CreateHorse();
        await SeedAsync(TestData.CreatePersonHorse(TestData.CreatePerson(), horse, isOwner: true));

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/riders/hidden", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonListResponse>(response);
        content.Persons.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetHiddenRiders_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{TestData.NonExistingId}/riders/hidden",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddRider_NewRider_RiderIsListedAfterOwner()
    {
        var horse = TestData.CreateHorse();
        var owner = TestData.CreatePerson("Otto");
        var rider = TestData.CreatePerson("Ria", role: RoleName.Guest);
        await SeedAsync(TestData.CreatePersonHorse(owner, horse, isOwner: true), rider);

        var response = await ApiClient.PostAsync($"api/horses/{horse.Id}/riders/{rider.Id}", null,
                                                 TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ridersResponse = await ApiClient.GetAsync($"api/horses/{horse.Id}/riders", TestCancellationToken);
        var riders = await ReadContentAsync<PersonListResponse>(ridersResponse);
        riders.Persons.Select(p => p.Id).Should().Equal(owner.Id, rider.Id);
    }

    [Fact]
    public async ValueTask AddRider_HiddenRider_ReturnsConflict()
    {
        var horse = TestData.CreateHorse();
        var hiddenRider = TestData.CreatePerson("Hans");
        await SeedAsync(TestData.CreatePersonHorse(hiddenRider, horse, isHidden: true));

        var response = await ApiClient.PostAsync($"api/horses/{horse.Id}/riders/{hiddenRider.Id}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddRider_NonExistingPerson_ReturnsNotFound()
    {
        var horse = TestData.CreateHorse();
        await SeedAsync(horse);

        var response = await ApiClient.PostAsync($"api/horses/{horse.Id}/riders/{TestData.NonExistingId}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask HideRider_VisibleRider_RiderIsOnlyListedAsHidden()
    {
        var horse = TestData.CreateHorse();
        var owner = TestData.CreatePerson("Otto");
        var rider = TestData.CreatePerson("Ria");
        await SeedAsync(TestData.CreatePersonHorse(owner, horse, isOwner: true),
                        TestData.CreatePersonHorse(rider, horse));

        var response = await ApiClient.PutAsync($"api/horses/{horse.Id}/riders/{rider.Id}/hidden", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var riders = await ReadContentAsync<PersonListResponse>(
            await ApiClient.GetAsync($"api/horses/{horse.Id}/riders", TestCancellationToken));
        riders.Persons.Should().ContainSingle().Which.Id.Should().Be(owner.Id);
        var hiddenRiders = await ReadContentAsync<PersonListResponse>(
            await ApiClient.GetAsync($"api/horses/{horse.Id}/riders/hidden", TestCancellationToken));
        hiddenRiders.Persons.Should().ContainSingle().Which.Id.Should().Be(rider.Id);
    }

    [Fact]
    public async ValueTask HideRider_Owner_ReturnsConflict()
    {
        var horse = TestData.CreateHorse();
        var owner = TestData.CreatePerson("Otto");
        await SeedAsync(TestData.CreatePersonHorse(owner, horse, isOwner: true));

        var response = await ApiClient.PutAsync($"api/horses/{horse.Id}/riders/{owner.Id}/hidden", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask HideRider_PersonIsNoRider_ReturnsNotFound()
    {
        var horse = TestData.CreateHorse();
        var stranger = TestData.CreatePerson("Fremd");
        await SeedAsync(horse, stranger);

        var response = await ApiClient.PutAsync($"api/horses/{horse.Id}/riders/{stranger.Id}/hidden", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask ShowRider_HiddenRider_RiderIsListedAgain()
    {
        var horse = TestData.CreateHorse();
        var hiddenRider = TestData.CreatePerson("Hans");
        await SeedAsync(TestData.CreatePersonHorse(hiddenRider, horse, isHidden: true));

        var response = await ApiClient.DeleteAsync($"api/horses/{horse.Id}/riders/{hiddenRider.Id}/hidden",
                                                   TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var riders = await ReadContentAsync<PersonListResponse>(
            await ApiClient.GetAsync($"api/horses/{horse.Id}/riders", TestCancellationToken));
        riders.Persons.Should().ContainSingle().Which.Id.Should().Be(hiddenRider.Id);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_ValidData_ReturnsCreatedSaddle()
    {
        var horse = TestData.CreateHorse();
        var category = TestData.CreateSaddleCategory();
        await SeedAsync(horse, category);
        var request = new AddSaddleRequest("Dressursattel", category.Id);

        var response = await ApiClient.PostAsJsonAsync($"api/horses/{horse.Id}/saddles", request, JsonOptions,
                                                       TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<SaddleDto>(response);
        content.Should().Be(new SaddleDto(content.Id, "Dressursattel", horse.Id, category.Id, "Dressage"));

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var saddles = await ReadContentAsync<SaddleListResponse>(getResponse);
        saddles.Saddles.Should().ContainSingle().Which.Should().Be(content);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_NonExistingHorse_ReturnsNotFound()
    {
        var category = TestData.CreateSaddleCategory();
        await SeedAsync(category);
        var request = new AddSaddleRequest("Dressursattel", category.Id);

        var response = await ApiClient.PostAsJsonAsync($"api/horses/{TestData.NonExistingId}/saddles", request,
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_NonExistingCategory_ReturnsNotFound()
    {
        var horse = TestData.CreateHorse();
        await SeedAsync(horse);
        var request = new AddSaddleRequest("Dressursattel", TestData.NonExistingId);

        var response = await ApiClient.PostAsJsonAsync($"api/horses/{horse.Id}/saddles", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_InvalidHorseId_ReturnsBadRequest()
    {
        var request = new AddSaddleRequest("Dressursattel", 1);

        var response = await ApiClient.PostAsJsonAsync("api/horses/-1/saddles", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_EmptyName_ReturnsBadRequest()
    {
        var request = new AddSaddleRequest("", 1);

        var response = await ApiClient.PostAsJsonAsync("api/horses/1/saddles", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask AddSaddleToHorse_InvalidCategoryId_ReturnsBadRequest()
    {
        var request = new AddSaddleRequest("Dressursattel", 0);

        var response = await ApiClient.PostAsJsonAsync("api/horses/1/saddles", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetAllSaddleCategories_ReturnsCategories()
    {
        var dressage = TestData.CreateSaddleCategory("Dressage");
        var jumping = TestData.CreateSaddleCategory("Jumping");
        await SeedAsync(dressage, jumping);

        var response = await ApiClient.GetAsync("api/horses/saddles/categories", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<SaddleCategoryListResponse>(response);
        content.Categories.Should().Contain([
            new SaddleCategoryDto(dressage.Id, "Dressage"),
            new SaddleCategoryDto(jumping.Id, "Jumping")
        ]);
    }
}