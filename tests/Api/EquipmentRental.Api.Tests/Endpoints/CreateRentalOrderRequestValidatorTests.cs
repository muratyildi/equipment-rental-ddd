using EquipmentRental.Api.Endpoints;

namespace EquipmentRental.Api.Tests.Endpoints;

public sealed class CreateRentalOrderRequestValidatorTests
{
    public static TheoryData<IReadOnlyCollection<CreateRentalLineRequest?>?>
        InvalidLines =>
        new()
        {
            null,
            Array.Empty<CreateRentalLineRequest?>(),
            new CreateRentalLineRequest?[] { null },
        };

    [Theory]
    [MemberData(nameof(InvalidLines))]
    public void Validate_WithMissingOrMalformedLines_ReturnsValidationError(
        IReadOnlyCollection<CreateRentalLineRequest?>? lines)
    {
        var request = CreateRequest(lines);

        var errors = CreateRentalOrderRequestValidator.Validate(request);

        Assert.Contains("lines", errors);
    }

    [Fact]
    public void Validate_WithRentalLine_ReturnsNoErrors()
    {
        var request = CreateRequest(
        [
            new CreateRentalLineRequest(
                Guid.NewGuid(),
                1,
                125m,
                "TRY"),
        ]);

        var errors = CreateRentalOrderRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    private static CreateRentalOrderRequest CreateRequest(
        IReadOnlyCollection<CreateRentalLineRequest?>? lines) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 2),
            lines);
}
