using System.Collections.Generic;
using System.Linq;

namespace VPay.Ols.Processor.Models.Authorization;

public record AuthorizationFile(AuthorizationHeader Header, List<AuthorizationDetail> Details, AuthorizationTrailer Trailer)
{
    public AuthorizationFile Copy()
    {
        List<AuthorizationDetail> copyOfDetails = Details.ConvertAll(detail => detail.Copy());

        return new AuthorizationFile(Header.Copy(), copyOfDetails, Trailer.Copy());
    }
}
