using Api.Dto.Account;
using Api.Validators.Account;
using FluentAssertions;

namespace Api.UnitTests.Validators;

public class RegisterRequestDtoValidatorTests
{
    private readonly RegisterRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var request = new RegisterRequestDto("", "mateus@teste.com", "12345678");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(x => x.ErrorMessage)
            .Should()
            .Contain("Nome é obrigatório.");
    }

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        var request = new RegisterRequestDto("Mateus", "mateus@teste.com", "12345678");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}