namespace IsoTreatmentProcessSupportAPI.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool EmailConfirmed { get; set; }
        public int Weight { get; set; }
        public int ClimaxDoseInMiligramsPerKilogramOfBodyWeight { get; set; }
        public int DailyDose { get; set; }
        public DateTime MedicationStartDate { get; set; }

        public string? ResetPasswordToken { get; set; }
        public virtual ICollection<Entry> Entries { get; set; }
    }
}
