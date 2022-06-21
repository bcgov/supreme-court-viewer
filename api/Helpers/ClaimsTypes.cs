using System.Collections.Generic;
using System.Security.Claims;

namespace Scv.Api.Helpers
{
    public class CustomClaimTypes
    {
        public const string ApplicationCode = nameof(CustomClaimTypes) + nameof(ApplicationCode);
        public const string JcParticipantId = nameof(CustomClaimTypes) + nameof(JcParticipantId);
        public const string JcAgencyCode = nameof(CustomClaimTypes) + nameof(JcAgencyCode);
        public const string IsSupremeUser = nameof(CustomClaimTypes) + nameof(IsSupremeUser);
        public const string CivilFileAccess = nameof(CustomClaimTypes) + nameof(CivilFileAccess);
        public const string Groups = "groups";
        public const string PreferredUsername = "preferred_username";

        public static List<string> UsedKeycloakClaimTypes = new List<string>
        {
            ClaimTypes.NameIdentifier,
            "idir_userid",
            "name",
            "preferred_username",
            "groups"
        };
    }
}
