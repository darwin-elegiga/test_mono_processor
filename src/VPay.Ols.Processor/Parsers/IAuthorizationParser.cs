using System.IO;
using VPay.Ols.Processor.Models.Authorization;

namespace VPay.Ols.Processor.Parsers;

public interface IAuthorizationParser
{
    AuthorizationFile ParseFile(StreamReader fileContent);
}
