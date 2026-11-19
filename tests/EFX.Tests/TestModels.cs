namespace EFX.Tests.Support
{
    public sealed class Address
    {
        public string Street { get; set; } = string.Empty;
    }
}

namespace EFX.Tests.Models
{
    public sealed class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }

    public sealed class Contact
    {
        public int Id { get; set; }
        public EFX.Tests.Support.Address Address { get; set; } = new();
    }

    public sealed class DuplicateColumns
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("Shared")]
        public string First { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Schema.Column("Shared")]
        public string Second { get; set; } = string.Empty;
    }
}
