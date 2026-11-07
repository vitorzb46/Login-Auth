using LoginAuth.ErrorMessages;

namespace LoginAuth.Properties;

static class ConfigurationStrings
{
    extension(IConfiguration configuration)
    {
        public string ConnectionString() =>
            configuration.GetValue<string>("DbInfo:ConnectionString")
                ?? throw new ArgumentNullException(nameof(configuration), SystemErrorMessage.ConnectionStringNotFound);
        public string DbFilePath() =>
            configuration.GetValue<string>("DbInfo:FilePath")
                ?? throw new ArgumentNullException(nameof(configuration), SystemErrorMessage.DbPathNotFound);
    }
}