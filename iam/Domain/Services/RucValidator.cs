namespace Jobsy.UserAuthentication.Domain.Services;

public static class RucValidator
{
    private static readonly int[] Factors = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

    public static bool IsValidCompanyRuc(string? ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return false;

        ruc = ruc.Trim();

        if (ruc.Length != 11 || !ruc.All(char.IsDigit) || !ruc.StartsWith("20"))
            return false;

        var sum = 0;
        for (var i = 0; i < Factors.Length; i++)
        {
            sum += (ruc[i] - '0') * Factors[i];
        }

        var checkDigit = 11 - (sum % 11);
        if (checkDigit == 10)
            checkDigit = 0;
        else if (checkDigit == 11)
            checkDigit = 1;

        return checkDigit == ruc[10] - '0';
    }
}
