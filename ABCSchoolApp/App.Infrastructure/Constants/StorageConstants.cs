namespace App.Infrastructure.Constants
{
    public static class StorageConstants
    {
        public static string AuthToken = "jwt";
        public static string RefreshToken = "refreshToken";

        // « Se souvenir de moi » — on ne persiste JAMAIS le mot de passe.
        public static string RememberMe = "rememberMe";
        public static string RememberedTenant = "rememberedTenant";
        public static string RememberedUsername = "rememberedUsername";
    }
}
