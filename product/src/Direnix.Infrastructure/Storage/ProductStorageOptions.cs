namespace Direnix.Infrastructure.Storage;

public sealed class ProductStorageOptions
{
    public const string SectionName = "Direnix:Storage";

    public string DataRoot { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Direnix", "Product", "data");

    public string DatabaseFileName { get; init; } = "direnix.adcx";

    public string KeyFileName { get; init; } = "direnix.dbkey.dpapi";

    public string DatabasePath => Path.Combine(DataRoot, DatabaseFileName);

    public string KeyPath => Path.Combine(DataRoot, KeyFileName);
}
