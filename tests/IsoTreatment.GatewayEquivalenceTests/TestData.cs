namespace IsoTreatment.GatewayEquivalenceTests;

// Seeded identically on both sides, ids included, so assertions can name exact values.
public static class TestData
{
    public const int OwnerId = 1;
    public const int OtherUserId = 2;
    public const int UserWithoutRemindersId = 3;

    public const int OwnersMorningReminderId = 1;
    public const int OwnersEveningReminderId = 2;
    public const int OtherUsersReminderId = 3;

    public const int MissingReminderId = 999;

    public static readonly TimeOnly OwnersMorningTime = new(8, 0);
    public static readonly TimeOnly OwnersEveningTime = new(20, 30);
    public static readonly TimeOnly OtherUsersTime = new(21, 15);

    // Both identity columns are reseeded to this value, so the next reminder added on either
    // side gets NextReminderId.
    public const int IdentitySeed = 100;
    public const int NextReminderId = IdentitySeed + 1;
}
