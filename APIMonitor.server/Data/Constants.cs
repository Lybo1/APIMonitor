namespace APIMonitor.server.Data
{
    public static class Constants
    {
        public const int DefaultAccessTokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 60;
        public const int RefreshTokenLength = 500;
        public const int RememberMeAccessTokenExpirationMinutes = 43200;
        
        public const int LongitudeLength = 50;
        public const int LatitudeLength = 50;
        public const int DescriptionLength = 100;
        public const int CountryLength = 100;
        public const int TitleLength = 200;
        public const int DetailsLength = 1000;
        
        public const int FirstNameMinLength = 3;
        public const int LastNameMinLength = 3;
        public const int MaxLoginAttempts = 3;
        public const int PasswordMinLength = 6;
        public const int UsernameMaxLength = 50;
        public const int FirstNameMaxLength = 50;
        public const int LastNameMaxLength = 50;
        public const int PasswordMaxLength = 100;
        
        public const int DefaultPageSize = 50;
        public const int DefaultErrorSpikeThreshold = 50;
        public const int MaxPageSize = 100;
        public const int DefaultRateLimitThreshold = 1000;

        public const int HttpMethodLength = 10;
        public const int Ipv4AddressLength = 15;
        public const int MacAddressLength = 17;
        public const int Ipv6AddressLength = 39;
        public const int EndPointLength = 200;
        public const int BotSignatureLength = 200;
    }
}