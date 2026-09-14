namespace Social.Core
{
    public static class VisibilityValues
    {
        public const string Public = "public";
        public const string Private = "private";

        [Obsolete("Use Public instead.")]
        public const string PUBLIC = Public;
        [Obsolete("Use Private instead.")]
        public const string PRIVATE = Private;
    }
}
