using System;
namespace Helpshift
{
    /// <summary>
    /// The reasons for the failure in authenticating the user
    /// </summary>
    public enum HelpshiftAuthenticationFailureReason
    {
        AUTH_TOKEN_NOT_PROVIDED = 0,
        INVALID_AUTH_TOKEN,
        UNKNOWN
    }
}