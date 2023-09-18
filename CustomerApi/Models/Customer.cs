namespace CustomerApi.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public int RegistrationNumber { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string AddressBilling { get; set; }
        public string AddressShipping { get; set; }
        public bool CreditStanding { get; set; }

    }
}
