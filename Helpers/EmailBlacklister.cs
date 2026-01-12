namespace ParrotsAPI2.Helpers;

public static class EmailBlacklister
{
    private static readonly HashSet<string> BlacklistedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com",
        "10minutemail.com",
        "guerrillamail.com",
        "tempmail.com",
        "yopmail.com",
        "trashmail.com",
        "maildrop.cc",
        "fakeinbox.com",
        "getnada.com",
        "dispostable.com",
        "temp-mail.org",
        "mintemail.com",
        "mailcatch.com",
        "spamgourmet.com",
        "dropmail.me",
        "sharklasers.com",
        "moakt.com",
        "throwawaymail.com",
        "spambog.com",
        "anonymbox.com",
        "mailnesia.com",
        "tempmail.net",
        "mail-temp.com",
        "tempinbox.com",
        "discard.email",
        "grr.la",
        "spam4.me",
        "fastmailinbox.com",
        "mailnull.com",
        "tempemail.co",
        "trashmail.net",
        "spambox.us",
        "wegwerfemail.de",
        "mail-temporaire.com",
        "tmail.com",
        "mailtome.de",
        "junkmail.com",
        "fake-mail.net",
        "oneoffemail.com",
        "10minutemail.net",
        "getairmail.com"
    };

    /// <summary>
    /// Checks if the email's domain is blacklisted (disposable).
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if blacklisted, false otherwise</returns>
    public static bool IsBlacklisted(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true; // treat empty/null as invalid

        email = email.Trim().ToLowerInvariant();

        int atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
            return true; // invalid email format

        string domain = email[(atIndex + 1)..];
        return BlacklistedDomains.Contains(domain);
    }
}
