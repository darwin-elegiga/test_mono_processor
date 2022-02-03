using VPay.Ols.Processor.Models.Authorization;

namespace VPay.Ols.Processor.Writers;

public interface IAuthorizationFileWriter
{
    public string WriteAuthorizationFile(AuthorizationFile file);
}
