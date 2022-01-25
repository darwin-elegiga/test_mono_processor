using Moq;

namespace VPay.Ols.Processor.Tests;
public sealed class ArgumentCaptor<TParameter>
{
#nullable disable
    public TParameter Value { get; private set; }
#nullable restore

    public int Calls { get; private set; }

    public TParameter Capture()
    {
        return It.Is<TParameter>(t => SaveValue(t));
    }

    public bool SaveValue(TParameter parameter)
    {
        Value = parameter;
        Calls++;
        return true;
    }

    public TCast? ValueAs<TCast>() where TCast : class
    {
        return Value as TCast;
    }
}
