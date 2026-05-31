namespace Domain.ValueObjects;

/// <summary>عنوان بريدي للعميل (BSS party).</summary>
public sealed class PostalAddress
{
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }
    public string? Country { get; private set; }

    private PostalAddress() { }

    public PostalAddress(string? street, string? city, string? state, string? zipCode, string? country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static PostalAddress Empty => new();
}
